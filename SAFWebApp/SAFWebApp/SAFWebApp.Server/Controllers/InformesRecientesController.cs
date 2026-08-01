using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/informes/recientes")]
    [ApiController]
    public class InformesRecientesController : ControllerBase
    {
        private readonly string _connectionString;

        private static readonly string[] TiposValidos =
        [
            "Completo",
            "Fugas",
            "Valvulas"
        ];

        private static readonly string[] SeccionesValidas =
        [
            "Todas",
            "Entrada",
            "Tramo_Izquierdo",
            "Tramo_Derecho",
            "Parte_Abajo",
            "Salida"
        ];

        public InformesRecientesController(
            IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString(
                    "DefaultConnection"
                )
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );
        }

        [HttpGet]
        public async Task<ActionResult<List<InformeGeneradoDto>>>
            ObtenerRecientes(
                [FromQuery] int limite = 10,
                CancellationToken cancellationToken = default)
        {
            int limiteNormalizado =
                Math.Clamp(limite, 1, 50);

            const string consulta = """
                SELECT
                    id,
                    tipo_informe,
                    fecha_inicio,
                    fecha_fin,
                    seccion,
                    nombre_archivo,
                    tamano_bytes,
                    usuario_id,
                    fecha_generacion

                FROM informes_generados

                ORDER BY fecha_generacion DESC, id DESC

                LIMIT @limite
                """;

            try
            {
                var informes =
                    new List<InformeGeneradoDto>();

                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                await using var comando =
                    new MySqlCommand(consulta, conexion)
                    {
                        CommandTimeout = 15
                    };

                comando.Parameters.AddWithValue(
                    "@limite",
                    limiteNormalizado
                );

                await using var reader =
                    await comando.ExecuteReaderAsync(
                        cancellationToken
                    );

                while (
                    await reader.ReadAsync(
                        cancellationToken
                    )
                )
                {
                    string tipoInforme =
                        LeerTexto(
                            reader,
                            "tipo_informe"
                        );

                    string seccion =
                        LeerTexto(
                            reader,
                            "seccion"
                        );

                    informes.Add(
                        new InformeGeneradoDto
                        {
                            Id =
                                LeerEntero(
                                    reader,
                                    "id"
                                ),

                            TipoInforme =
                                tipoInforme,

                            TipoInformeEtiqueta =
                                ObtenerEtiquetaTipo(
                                    tipoInforme
                                ),

                            FechaInicio =
                                LeerFecha(
                                    reader,
                                    "fecha_inicio",
                                    "yyyy-MM-dd"
                                ),

                            FechaFin =
                                LeerFecha(
                                    reader,
                                    "fecha_fin",
                                    "yyyy-MM-dd"
                                ),

                            Seccion =
                                seccion,

                            SeccionEtiqueta =
                                ObtenerEtiquetaSeccion(
                                    seccion
                                ),

                            NombreArchivo =
                                LeerTexto(
                                    reader,
                                    "nombre_archivo"
                                ),

                            TamanoBytes =
                                LeerLong(
                                    reader,
                                    "tamano_bytes"
                                ),

                            UsuarioId =
                                LeerEnteroNullable(
                                    reader,
                                    "usuario_id"
                                ),

                            FechaGeneracion =
                                LeerFecha(
                                    reader,
                                    "fecha_generacion",
                                    "yyyy-MM-dd HH:mm:ss"
                                )
                        }
                    );
                }

                return Ok(informes);
            }
            catch (MySqlException error)
            {
                Console.Error.WriteLine(
                    $"Error al consultar informes recientes: {error}"
                );

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error =
                            "No fue posible consultar los informes recientes"
                    }
                );
            }
        }

        [HttpPost]
        public async Task<ActionResult<RegistroInformeRespuestaDto>>
            RegistrarInforme(
                [FromBody] RegistrarInformeGeneradoDto solicitud,
                CancellationToken cancellationToken = default)
        {
            string tipoInforme =
                NormalizarTipoInforme(
                    solicitud.TipoInforme
                );

            string seccion =
                NormalizarSeccion(
                    solicitud.Seccion
                );

            if (string.IsNullOrWhiteSpace(tipoInforme))
            {
                return BadRequest(new
                {
                    error =
                        "El tipo de informe no es válido"
                });
            }

            if (string.IsNullOrWhiteSpace(seccion))
            {
                return BadRequest(new
                {
                    error =
                        "La sección seleccionada no es válida"
                });
            }

            if (
                !DateOnly.TryParseExact(
                    solicitud.FechaInicio,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly fechaInicio
                )
            )
            {
                return BadRequest(new
                {
                    error =
                        "La fecha inicial no es válida"
                });
            }

            if (
                !DateOnly.TryParseExact(
                    solicitud.FechaFin,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateOnly fechaFin
                )
            )
            {
                return BadRequest(new
                {
                    error =
                        "La fecha final no es válida"
                });
            }

            if (fechaFin < fechaInicio)
            {
                return BadRequest(new
                {
                    error =
                        "La fecha final no puede ser menor que la fecha inicial"
                });
            }

            string nombreArchivo =
                solicitud.NombreArchivo.Trim();

            if (string.IsNullOrWhiteSpace(nombreArchivo))
            {
                return BadRequest(new
                {
                    error =
                        "El nombre del archivo es obligatorio"
                });
            }

            if (
                !nombreArchivo.EndsWith(
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                nombreArchivo += ".pdf";
            }

            const string consulta = """
                INSERT INTO informes_generados
                (
                    tipo_informe,
                    fecha_inicio,
                    fecha_fin,
                    seccion,
                    nombre_archivo,
                    tamano_bytes,
                    usuario_id,
                    fecha_generacion
                )
                VALUES
                (
                    @tipoInforme,
                    @fechaInicio,
                    @fechaFin,
                    @seccion,
                    @nombreArchivo,
                    @tamanoBytes,
                    @usuarioId,
                    CURRENT_TIMESTAMP
                );

                SELECT LAST_INSERT_ID();
                """;

            try
            {
                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                await using var comando =
                    new MySqlCommand(consulta, conexion)
                    {
                        CommandTimeout = 15
                    };

                comando.Parameters.AddWithValue(
                    "@tipoInforme",
                    tipoInforme
                );

                comando.Parameters.AddWithValue(
                    "@fechaInicio",
                    fechaInicio.ToDateTime(
                        TimeOnly.MinValue
                    )
                );

                comando.Parameters.AddWithValue(
                    "@fechaFin",
                    fechaFin.ToDateTime(
                        TimeOnly.MinValue
                    )
                );

                comando.Parameters.AddWithValue(
                    "@seccion",
                    seccion
                );

                comando.Parameters.AddWithValue(
                    "@nombreArchivo",
                    nombreArchivo
                );

                comando.Parameters.AddWithValue(
                    "@tamanoBytes",
                    Math.Max(
                        solicitud.TamanoBytes,
                        0
                    )
                );

                comando.Parameters.AddWithValue(
                    "@usuarioId",
                    solicitud.UsuarioId
                    ?? (object)DBNull.Value
                );

                object? resultado =
                    await comando.ExecuteScalarAsync(
                        cancellationToken
                    );

                int id =
                    resultado is null
                    || resultado == DBNull.Value
                        ? 0
                        : Convert.ToInt32(
                            resultado,
                            CultureInfo.InvariantCulture
                        );

                return Ok(
                    new RegistroInformeRespuestaDto
                    {
                        Ok = id > 0,
                        Id = id
                    }
                );
            }
            catch (MySqlException error)
            {
                Console.Error.WriteLine(
                    $"Error al registrar el informe: {error}"
                );

                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error =
                            "No fue posible registrar el informe generado"
                    }
                );
            }
        }

        private static string NormalizarTipoInforme(
            string tipo)
        {
            string valor =
                tipo?.Trim() ?? string.Empty;

            return TiposValidos.FirstOrDefault(
                tipoValido =>
                    tipoValido.Equals(
                        valor,
                        StringComparison.OrdinalIgnoreCase
                    )
            ) ?? string.Empty;
        }

        private static string NormalizarSeccion(
            string seccion)
        {
            string valor =
                seccion?.Trim() ?? string.Empty;

            return SeccionesValidas.FirstOrDefault(
                seccionValida =>
                    seccionValida.Equals(
                        valor,
                        StringComparison.OrdinalIgnoreCase
                    )
            ) ?? string.Empty;
        }

        private static string ObtenerEtiquetaTipo(
            string tipo)
        {
            return tipo switch
            {
                "Completo" =>
                    "Informe completo",

                "Fugas" =>
                    "Análisis de fugas",

                "Valvulas" =>
                    "Estado de válvulas",

                _ =>
                    tipo
            };
        }

        private static string ObtenerEtiquetaSeccion(
            string seccion)
        {
            return seccion switch
            {
                "Todas" =>
                    "Todas las secciones",

                "Tramo_Izquierdo" =>
                    "Tramo Izquierdo",

                "Tramo_Derecho" =>
                    "Tramo Derecho",

                "Parte_Abajo" =>
                    "Parte Abajo",

                _ =>
                    seccion.Replace("_", " ")
            };
        }

        private static string LeerTexto(
            MySqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return string.Empty;
            }

            return Convert.ToString(
                reader.GetValue(posicion),
                CultureInfo.InvariantCulture
            ) ?? string.Empty;
        }

        private static int LeerEntero(
            MySqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return 0;
            }

            return Convert.ToInt32(
                reader.GetValue(posicion),
                CultureInfo.InvariantCulture
            );
        }

        private static int? LeerEnteroNullable(
            MySqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return null;
            }

            return Convert.ToInt32(
                reader.GetValue(posicion),
                CultureInfo.InvariantCulture
            );
        }

        private static long LeerLong(
            MySqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return 0;
            }

            return Convert.ToInt64(
                reader.GetValue(posicion),
                CultureInfo.InvariantCulture
            );
        }

        private static string LeerFecha(
            MySqlDataReader reader,
            string columna,
            string formato)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return string.Empty;
            }

            object valor =
                reader.GetValue(posicion);

            if (valor is DateTime fecha)
            {
                return fecha.ToString(
                    formato,
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