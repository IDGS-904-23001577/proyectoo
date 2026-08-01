using System.Diagnostics;

using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EstadoApiController : ControllerBase
{
    private readonly string _connectionString;
    private readonly IWebHostEnvironment _environment;

    public EstadoApiController(
        IConfiguration configuration,
        IWebHostEnvironment environment
    )
    {
        _connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión DefaultConnection"
            );

        _environment = environment;
    }

    /*
     * GET: /api/estadoapi
     */
    [HttpGet]
    public async Task<ActionResult<EstadoApiDto>> Obtener(
        CancellationToken cancellationToken
    )
    {
        var reloj = Stopwatch.StartNew();

        bool baseDatos = false;

        try
        {
            await using var conexion =
                new MySqlConnection(_connectionString);

            await conexion.OpenAsync(cancellationToken);

            await using var comando =
                new MySqlCommand("SELECT 1", conexion)
                {
                    CommandTimeout = 8
                };

            object? resultado =
                await comando.ExecuteScalarAsync(cancellationToken);

            baseDatos =
                Convert.ToInt32(resultado) == 1;
        }
        catch (MySqlException)
        {
            baseDatos = false;
        }

        reloj.Stop();

        return Ok(
            new EstadoApiDto
            {
                Estado = "Operativa",
                Version = "1.0.0",
                Ambiente = _environment.EnvironmentName,
                BaseDatos = baseDatos,
                FechaServidor =
                    DateTimeOffset.Now.ToString("O"),
                TiempoRespuestaMs =
                    reloj.ElapsedMilliseconds,
                Endpoints = CrearEndpoints()
            }
        );
    }

    private static List<EndpointApiDto> CrearEndpoints()
    {
        return
        [
            E(
                "POST",
                "/api/auth/login",
                "Autenticación de usuarios",
                "Seguridad"
            ),

            E(
                "GET",
                "/api/dashboard",
                "Resumen general del sistema",
                "Dashboard"
            ),

            E(
                "GET",
                "/api/lecturas/actual",
                "Lecturas actuales de sensores",
                "En vivo"
            ),

            E(
                "GET",
                "/api/lecturas/historial",
                "Historial reciente de lecturas",
                "En vivo"
            ),

            E(
                "GET",
                "/api/red",
                "Estado visual de la red",
                "Red"
            ),

            E(
                "GET",
                "/api/alertas",
                "Listado de fugas y alertas",
                "Alertas"
            ),

            E(
                "GET",
                "/api/predicciones",
                "Predicciones de riesgo",
                "Predicciones"
            ),

            E(
                "GET",
                "/api/informes/catalogo",
                "Catálogos para informes",
                "Informes"
            ),

            E(
                "GET",
                "/api/valvulas",
                "Estado de las válvulas",
                "Válvulas"
            ),

            E(
                "POST",
                "/api/valvulas/{numero}/comando",
                "Abrir o cerrar una válvula",
                "Válvulas"
            ),

            E(
                "GET",
                "/api/estadoapi",
                "Diagnóstico de la API",
                "API"
            )
        ];
    }

    private static EndpointApiDto E(
        string metodo,
        string ruta,
        string descripcion,
        string modulo
    )
    {
        return new EndpointApiDto
        {
            Metodo = metodo,
            Ruta = ruta,
            Descripcion = descripcion,
            Modulo = modulo
        };
    }
}