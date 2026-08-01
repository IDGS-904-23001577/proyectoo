using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RedController : ControllerBase
    {
        private readonly string _connectionString;

        public RedController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );
        }

        [HttpGet]
        public async Task<ActionResult<RedDistribucionDto>> ObtenerRed(
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT
                    id,
                    seccion,
                    fecha_deteccion,
                    severidad,
                    estado
                FROM fugas
                WHERE estado IN ('Activa', 'Pendiente')
                ORDER BY
                    CASE estado
                        WHEN 'Activa' THEN 1
                        WHEN 'Pendiente' THEN 2
                        ELSE 3
                    END,
                    CASE severidad
                        WHEN 'Alta' THEN 1
                        WHEN 'Media' THEN 2
                        WHEN 'Baja' THEN 3
                        ELSE 4
                    END,
                    fecha_deteccion DESC
                """;

            try
            {
                RedDistribucionDto respuesta = CrearMapaBase();

                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                await using var comando =
                    new MySqlCommand(consulta, conexion)
                    {
                        CommandTimeout = 15
                    };

                await using var reader =
                    await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    string seccion = LeerTexto(
                        reader,
                        "seccion"
                    );

                    string? nodoId = ObtenerNodoId(seccion);

                    if (nodoId is null)
                    {
                        continue;
                    }

                    NodoRedDto? nodo = respuesta.Nodos.FirstOrDefault(
                        elemento => elemento.Id == nodoId
                    );

                    if (nodo is null || nodo.Estado != "Normal")
                    {
                        continue;
                    }

                    string estadoFuga = LeerTexto(
                        reader,
                        "estado"
                    );

                    nodo.Estado = ObtenerEstadoVisual(estadoFuga);

                    nodo.EstadoFuga = estadoFuga;

                    nodo.Severidad = LeerTexto(
                        reader,
                        "severidad",
                        "Sin definir"
                    );

                    nodo.FugaId = LeerTexto(
                        reader,
                        "id"
                    );

                    nodo.SeccionAfectada = seccion;

                    nodo.FechaDeteccion = LeerFecha(
                        reader,
                        "fecha_deteccion"
                    );
                }

                return Ok(respuesta);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error =
                            "No fue posible consultar el estado de la red"
                    }
                );
            }
        }

        private static RedDistribucionDto CrearMapaBase()
        {
            return new RedDistribucionDto
            {
                FechaActualizacion = DateTimeOffset.UtcNow.ToString(
                    "O",
                    CultureInfo.InvariantCulture
                ),

                Nodos =
                [
                    new NodoRedDto
                    {
                        Id = "entrada",
                        Etiqueta = "Entrada",
                        PosicionX = 50,
                        PosicionY = 14
                    },
                    new NodoRedDto
                    {
                        Id = "tramo-izquierdo",
                        Etiqueta = "Tramo Izquierdo",
                        PosicionX = 25,
                        PosicionY = 46
                    },
                    new NodoRedDto
                    {
                        Id = "tramo-derecho",
                        Etiqueta = "Tramo Derecho",
                        PosicionX = 75,
                        PosicionY = 46
                    },
                    new NodoRedDto
                    {
                        Id = "salida",
                        Etiqueta = "Salida",
                        PosicionX = 50,
                        PosicionY = 78
                    }
                ],

                Conexiones =
                [
                    new ConexionRedDto
                    {
                        Origen = "entrada",
                        Destino = "tramo-izquierdo"
                    },
                    new ConexionRedDto
                    {
                        Origen = "entrada",
                        Destino = "tramo-derecho"
                    },
                    new ConexionRedDto
                    {
                        Origen = "tramo-izquierdo",
                        Destino = "salida"
                    },
                    new ConexionRedDto
                    {
                        Origen = "tramo-derecho",
                        Destino = "salida"
                    }
                ]
            };
        }

        private static string? ObtenerNodoId(string seccion)
        {
            string seccionNormalizada = seccion
                .Trim()
                .Replace(" ", "_")
                .ToLowerInvariant();

            return seccionNormalizada switch
            {
                "entrada" => "entrada",

                "tramo_izquierdo" =>
                    "tramo-izquierdo",

                "parte_abajo" =>
                    "tramo-izquierdo",

                "tramo_derecho" =>
                    "tramo-derecho",

                "salida" => "salida",

                _ => null
            };
        }

        private static string ObtenerEstadoVisual(
            string estadoFuga)
        {
            return estadoFuga.Trim().ToLowerInvariant() switch
            {
                "activa" => "Fuga",
                "pendiente" => "Advertencia",
                _ => "Normal"
            };
        }

        private static string LeerTexto(
            MySqlDataReader reader,
            string columna,
            string valorPredeterminado = "")
        {
            int posicion = reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return valorPredeterminado;
            }

            object valor = reader.GetValue(posicion);

            return Convert.ToString(
                valor,
                CultureInfo.InvariantCulture
            ) ?? valorPredeterminado;
        }

        private static string LeerFecha(
            MySqlDataReader reader,
            string columna)
        {
            int posicion = reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return string.Empty;
            }

            object valor = reader.GetValue(posicion);

            if (valor is DateTime fecha)
            {
                return fecha.ToString(
                    "yyyy-MM-dd HH:mm:ss",
                    CultureInfo.InvariantCulture
                );
            }

            return Convert.ToString(
                valor,
                CultureInfo.InvariantCulture
            ) ?? string.Empty;
        }
    }
}