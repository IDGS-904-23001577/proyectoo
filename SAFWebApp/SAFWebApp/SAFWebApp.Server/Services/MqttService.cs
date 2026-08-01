using MQTTnet;
using MQTTnet.Client;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.SignalR;

using SAFWebApp.Server.Hubs;

namespace SAFWebApp.Server.Services;

public class MqttService : IHostedService
{
    private IMqttClient? _cliente;
    private readonly IConfiguration _config;
    private readonly IHubContext<ValvulasHub> _hub;

    // Estado real confirmado por el ESP32 (fuente de verdad)
    public Dictionary<int, string> EstadosValvulas { get; } = new()
    {
        { 1, "Desconocido" },
        { 2, "Desconocido" },
        { 3, "Desconocido" }
    };

    private static readonly Dictionary<string, int> TopicAValvula = new()
    {
        { "siaf/valvula/1", 1 },
        { "siaf/valvula/2", 2 },
        { "siaf/valvula/3", 3 }
    };

    public MqttService(IConfiguration config, IHubContext<ValvulasHub> hub)
    {
        _config = config;
        _hub = hub;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new MqttFactory();
        _cliente = factory.CreateMqttClient();

        var opciones = new MqttClientOptionsBuilder()
            .WithTcpServer(_config["Mqtt:Host"], int.Parse(_config["Mqtt:Port"]!))
            .WithCredentials(_config["Mqtt:User"], _config["Mqtt:Password"])
            .WithTls()
            .Build();

        _cliente.ApplicationMessageReceivedAsync += async e =>
        {
            var topic = e.ApplicationMessage.Topic;
            if (!TopicAValvula.TryGetValue(topic, out int numero))
            {
                return;
            }

            var payload = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            // Los comandos "abrir"/"cerrar" que este mismo backend publica
            // regresan por eco del broker (misma suscripción). No son JSON,
            // así que se ignoran aquí — solo nos interesa la confirmación
            // del ESP32, que sí viene en formato {"estado":"abierta"}.
            if (!payload.TrimStart().StartsWith("{"))
            {
                return;
            }

            try
            {
                var json = JsonDocument.Parse(payload);
                if (json.RootElement.TryGetProperty("estado", out var estado))
                {
                    EstadosValvulas[numero] =
                        estado.GetString() == "abierta" ? "Abierta" : "Cerrada";

                    await _hub.Clients.All.SendAsync(
                        "ValvulaActualizada",
                        numero,
                        EstadosValvulas[numero]
                    );
                }
            }
            catch (JsonException)
            {
                // Mensaje no era JSON válido — se ignora, no es crítico.
            }
        };

        await _cliente.ConnectAsync(opciones, cancellationToken);

        foreach (var topic in TopicAValvula.Keys)
        {
            await _cliente.SubscribeAsync(topic);
        }
    }

    public async Task PublicarComando(int numeroValvula, string comando)
    {
        var topic = $"siaf/valvula/{numeroValvula}";
        var mensaje = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(comando) // "abrir" o "cerrar"
            .Build();
        await _cliente!.PublishAsync(mensaje);
    }

    public Task StopAsync(CancellationToken cancellationToken) =>
        _cliente?.DisconnectAsync() ?? Task.CompletedTask;
}