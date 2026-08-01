using System.Globalization;

using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

using SAFWebApp.Server.Models;
using SAFWebApp.Server.Services;

namespace SAFWebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ValvulasController : ControllerBase
{
    private readonly string _connectionString;
    private readonly MqttService _mqttService;

    public ValvulasController(IConfiguration configuration, MqttService mqttService)
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión DefaultConnection"
            );

        _mqttService = mqttService;
    }

    /*
     * GET: /api/valvulas
     *
     * Obtiene el catálogo y estado REAL de las tres válvulas,
     * confirmado por el ESP32 vía MQTT (no simulado).
     */
    [HttpGet]
    public async Task<ActionResult<List<ValvulaDto>>> Obtener(
        CancellationToken cancellationToken
    )
    {
        string ultimaLectura = string.Empty;

        try
        {
            await using var conexion =
                new MySqlConnection(_connectionString);

            await conexion.OpenAsync(cancellationToken);

            const string sql =
                "SELECT MAX(timestamp) FROM lecturas";

            await using var comando =
                new MySqlCommand(sql, conexion)
                {
                    CommandTimeout = 10
                };

            object? valor =
                await comando.ExecuteScalarAsync(cancellationToken);

            if (valor is DateTime fecha)
            {
                ultimaLectura = fecha.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }
        }
        catch (MySqlException)
        {
            /*
             * El módulo sigue funcionando aunque no se pueda
             * consultar la última lectura de sensores.
             */
        }

        return Ok(
            CrearCatalogo(ultimaLectura)
        );
    }

    /*
     * POST: /api/valvulas/1/comando
     *
     * Body:
     * {
     *   "estado": "abrir"
     * }
     *
     * Publica el comando por MQTT al ESP32. El estado que se
     * refleja después viene de la confirmación real del hardware,
     * no de este mismo request.
     */
    [HttpPost("{numero:int}/comando")]
    public async Task<ActionResult<RespuestaComandoValvulaDto>> Ejecutar(
        int numero,
        [FromBody] ComandoValvulaDto comando
    )
    {
        if (numero is < 1 or > 3)
        {
            return NotFound(
                new
                {
                    error = "La válvula indicada no existe."
                }
            );
        }

        string orden =
            comando.Estado
                .Trim()
                .ToLowerInvariant();

        if (orden is not ("abrir" or "cerrar"))
        {
            return BadRequest(
                new
                {
                    error =
                        "El comando debe ser abrir o cerrar."
                }
            );
        }

        await _mqttService.PublicarComando(numero, orden);

        // Nota: el estado mostrado aquí es el último conocido en el
        // momento de responder. La confirmación real llega por MQTT
        // unos instantes después y se refleja vía SignalR (o en el
        // siguiente GET) cuando el ESP32 la publique.
        ValvulaDto valvula =
            CrearCatalogo(
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            )
            .Single(v => v.Numero == numero);

        return Ok(
            new RespuestaComandoValvulaDto
            {
                Ok = true,
                Mensaje =
                    $"Comando '{orden}' enviado a la válvula {numero}.",
                Valvula = valvula
            }
        );
    }

    private List<ValvulaDto> CrearCatalogo(
        string fecha
    )
    {
        return
        [
            new ValvulaDto
            {
                Numero = 1,
                Nombre = "Válvula de entrada",
                Secciones = "Entrada y Tramo Derecho",
                Topic = "siaf/valvula/1",
                Estado = _mqttService.EstadosValvulas[1],
                UltimaActualizacion = fecha
            },

            new ValvulaDto
            {
                Numero = 2,
                Nombre = "Válvula del tramo izquierdo",
                Secciones = "Tramo Izquierdo y Parte de Abajo",
                Topic = "siaf/valvula/2",
                Estado = _mqttService.EstadosValvulas[2],
                UltimaActualizacion = fecha
            },

            new ValvulaDto
            {
                Numero = 3,
                Nombre = "Válvula de salida",
                Secciones = "Salida",
                Topic = "siaf/valvula/3",
                Estado = _mqttService.EstadosValvulas[3],
                UltimaActualizacion = fecha
            }
        ];
    }
}