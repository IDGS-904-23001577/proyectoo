using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;
using SAFWebApp.Server.Services;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class InformesController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly InformePdfService _informePdfService;

        private static readonly OpcionCatalogoDto[] TiposInforme =
        [
            new OpcionCatalogoDto
            {
                Valor = "Completo",
                Etiqueta = "Informe completo"
            },
            new OpcionCatalogoDto
            {
                Valor = "Fugas",
                Etiqueta = "Análisis de fugas"
            },
            new OpcionCatalogoDto
            {
                Valor = "Valvulas",
                Etiqueta = "Estado de válvulas"
            }
        ];

        private static readonly OpcionCatalogoDto[] Secciones =
        [
            new OpcionCatalogoDto
            {
                Valor = "Todas",
                Etiqueta = "Todas las secciones"
            },
            new OpcionCatalogoDto
            {
                Valor = "Entrada",
                Etiqueta = "Entrada"
            },
            new OpcionCatalogoDto
            {
                Valor = "Tramo_Izquierdo",
                Etiqueta = "Tramo Izquierdo"
            },
            new OpcionCatalogoDto
            {
                Valor = "Tramo_Derecho",
                Etiqueta = "Tramo Derecho"
            },
            new OpcionCatalogoDto
            {
                Valor = "Parte_Abajo",
                Etiqueta = "Parte Abajo"
            },
            new OpcionCatalogoDto
            {
                Valor = "Salida",
                Etiqueta = "Salida"
            }
        ];

        public InformesController(
    IConfiguration configuration,
    InformePdfService informePdfService)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );

            _informePdfService = informePdfService;
        }

        [HttpGet("catalogos")]
        public ActionResult<CatalogoInformesDto> ObtenerCatalogos()
        {
            return Ok(new CatalogoInformesDto
            {
                TiposInforme = TiposInforme.ToList(),
                Secciones = Secciones.ToList()
            });
        }

        [HttpGet("vista-previa")]
        public async Task<ActionResult<VistaPreviaInformeDto>>
            ObtenerVistaPrevia(
                [FromQuery] string tipo = "Completo",
                [FromQuery] DateOnly? fechaInicio = null,
                [FromQuery] DateOnly? fechaFin = null,
                [FromQuery] string seccion = "Todas",
                CancellationToken cancellationToken = default)
        {
            DateOnly fechaActual =
                DateOnly.FromDateTime(DateTime.Now);

            DateOnly inicio =
                fechaInicio
                ?? new DateOnly(
                    fechaActual.Year,
                    fechaActual.Month,
                    1
                );

            DateOnly fin =
                fechaFin
                ?? fechaActual;

            string tipoNormalizado =
                NormalizarTipoInforme(tipo);

            string seccionNormalizada =
                NormalizarSeccion(seccion);

            if (fin < inicio)
            {
                return BadRequest(new
                {
                    error =
                        "La fecha final no puede ser menor que la fecha inicial"
                });
            }

            if (string.IsNullOrEmpty(tipoNormalizado))
            {
                return BadRequest(new
                {
                    error = "El tipo de informe no es válido"
                });
            }

            if (string.IsNullOrEmpty(seccionNormalizada))
            {
                return BadRequest(new
                {
                    error = "La sección seleccionada no es válida"
                });
            }

            DateTime fechaInicioConsulta =
                inicio.ToDateTime(TimeOnly.MinValue);

            DateTime fechaFinExclusiva =
                fin.ToDateTime(TimeOnly.MinValue).AddDays(1);

            try
            {
                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                ResumenInformeDto resumen =
                    await ObtenerResumenFugasAsync(
                        conexion,
                        fechaInicioConsulta,
                        fechaFinExclusiva,
                        seccionNormalizada,
                        cancellationToken
                    );

                resumen.TotalIntervenciones =
                    await ObtenerTotalIntervencionesAsync(
                        conexion,
                        fechaInicioConsulta,
                        fechaFinExclusiva,
                        seccionNormalizada,
                        cancellationToken
                    );

                await CompletarResumenLecturasAsync(
                    conexion,
                    resumen,
                    fechaInicioConsulta,
                    fechaFinExclusiva,
                    seccionNormalizada,
                    cancellationToken
                );

                List<EstadoValvulaInformeDto> valvulas =
                    await ObtenerEstadosValvulasAsync(
                        conexion,
                        fechaInicioConsulta,
                        fechaFinExclusiva,
                        seccionNormalizada,
                        cancellationToken
                    );

                resumen.ValvulasAbiertas =
                    valvulas.Count(
                        valvula => valvula.Estado == "Abierta"
                    );

                resumen.ValvulasCerradas =
                    valvulas.Count(
                        valvula => valvula.Estado == "Cerrada"
                    );

                var respuesta = new VistaPreviaInformeDto
                {
                    TipoInforme = tipoNormalizado,

                    TipoInformeEtiqueta =
                        ObtenerEtiquetaTipo(tipoNormalizado),

                    FechaInicio = inicio.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    ),

                    FechaFin = fin.ToString(
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture
                    ),

                    Seccion = seccionNormalizada,

                    SeccionEtiqueta =
                        ObtenerEtiquetaSeccion(seccionNormalizada),

                    Resumen = resumen,

                    SeccionesIncluidas =
                        ObtenerSeccionesIncluidas(
                            seccionNormalizada
                        ),

                    Valvulas = valvulas,

                    FechaGeneracion =
                        DateTimeOffset.UtcNow.ToString(
                            "O",
                            CultureInfo.InvariantCulture
                        )
                };

                return Ok(respuesta);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error =
                            "No fue posible consultar la información del informe"
                    }
                );
            }
        }

        [HttpGet("pdf")]
        public async Task<IActionResult> DescargarPdf(
    [FromQuery] string tipo = "Completo",
    [FromQuery] DateOnly? fechaInicio = null,
    [FromQuery] DateOnly? fechaFin = null,
    [FromQuery] string seccion = "Todas",
    CancellationToken cancellationToken = default)
        {
            ActionResult<VistaPreviaInformeDto> resultado =
                await ObtenerVistaPrevia(
                    tipo,
                    fechaInicio,
                    fechaFin,
                    seccion,
                    cancellationToken
                );

            VistaPreviaInformeDto? informe =
                resultado.Value;

            if (
                informe is null
                && resultado.Result is OkObjectResult resultadoCorrecto
            )
            {
                informe =
                    resultadoCorrecto.Value
                    as VistaPreviaInformeDto;
            }

            if (informe is null)
            {
                if (resultado.Result is IActionResult resultadoError)
                {
                    return resultadoError;
                }

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        error =
                            "No fue posible preparar la información del PDF"
                    }
                );
            }

            try
            {
                byte[] archivoPdf =
                    _informePdfService.GenerarPdf(informe);

                string tipoArchivo =
                    informe.TipoInforme
                        .Trim()
                        .ToLowerInvariant();

                string seccionArchivo =
                    informe.Seccion
                        .Trim()
                        .ToLowerInvariant()
                        .Replace("_", "-");

                string nombreArchivo =
                    $"saf-{tipoArchivo}-{seccionArchivo}-" +
                    $"{informe.FechaInicio}-{informe.FechaFin}.pdf";

                return File(
                    archivoPdf,
                    "application/pdf",
                    nombreArchivo
                );
            }
            catch (Exception error)
            {
                Console.Error.WriteLine(
                    $"Error al generar el PDF: {error}"
                );

                return StatusCode(
                    StatusCodes.Status500InternalServerError,
                    new
                    {
                        error =
                            "No fue posible generar el archivo PDF"
                    }
                );
            }
        }

        private static async Task<ResumenInformeDto>
            ObtenerResumenFugasAsync(
                MySqlConnection conexion,
                DateTime fechaInicio,
                DateTime fechaFinExclusiva,
                string seccion,
                CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT
                    COUNT(*) AS total_fugas,

                    COALESCE(
                        SUM(
                            CASE
                                WHEN estado = 'Activa' THEN 1
                                ELSE 0
                            END
                        ),
                        0
                    ) AS fugas_activas,

                    COALESCE(
                        SUM(
                            CASE
                                WHEN estado = 'Pendiente' THEN 1
                                ELSE 0
                            END
                        ),
                        0
                    ) AS fugas_pendientes,

                    COALESCE(
                        SUM(
                            CASE
                                WHEN estado = 'Resuelta' THEN 1
                                ELSE 0
                            END
                        ),
                        0
                    ) AS fugas_resueltas,

                    COALESCE(
                        SUM(volumen_perdido),
                        0
                    ) AS volumen_perdido,

                    COUNT(
                        DISTINCT seccion
                    ) AS secciones_afectadas

                FROM fugas

                WHERE fecha_deteccion >= @fechaInicio
                  AND fecha_deteccion < @fechaFinExclusiva
                  AND (
                      @filtrarSeccion = 0
                      OR seccion = @seccion
                  )
                """;

            await using var comando =
                new MySqlCommand(consulta, conexion)
                {
                    CommandTimeout = 15
                };

            AgregarParametros(
                comando,
                fechaInicio,
                fechaFinExclusiva,
                seccion
            );

            await using var reader =
                await comando.ExecuteReaderAsync(
                    cancellationToken
                );

            if (!await reader.ReadAsync(cancellationToken))
            {
                return new ResumenInformeDto();
            }

            return new ResumenInformeDto
            {
                TotalFugas =
                    LeerEntero(reader, "total_fugas"),

                FugasActivas =
                    LeerEntero(reader, "fugas_activas"),

                FugasPendientes =
                    LeerEntero(reader, "fugas_pendientes"),

                FugasResueltas =
                    LeerEntero(reader, "fugas_resueltas"),

                VolumenPerdidoLitros =
                    LeerDecimal(reader, "volumen_perdido"),

                SeccionesAfectadas =
                    LeerEntero(reader, "secciones_afectadas")
            };
        }

        private static async Task<int>
            ObtenerTotalIntervencionesAsync(
                MySqlConnection conexion,
                DateTime fechaInicio,
                DateTime fechaFinExclusiva,
                string seccion,
                CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT COUNT(*) AS total_intervenciones

                FROM intervenciones i

                INNER JOIN fugas f
                    ON f.id = i.fuga_id

                WHERE i.hora_llegada >= @fechaInicio
                  AND i.hora_llegada < @fechaFinExclusiva
                  AND (
                      @filtrarSeccion = 0
                      OR f.seccion = @seccion
                  )
                """;

            await using var comando =
                new MySqlCommand(consulta, conexion)
                {
                    CommandTimeout = 15
                };

            AgregarParametros(
                comando,
                fechaInicio,
                fechaFinExclusiva,
                seccion
            );

            object? resultado =
                await comando.ExecuteScalarAsync(
                    cancellationToken
                );

            return resultado is null
                || resultado == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        resultado,
                        CultureInfo.InvariantCulture
                    );
        }

        private static async Task
            CompletarResumenLecturasAsync(
                MySqlConnection conexion,
                ResumenInformeDto resumen,
                DateTime fechaInicio,
                DateTime fechaFinExclusiva,
                string seccion,
                CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT
                    COUNT(*) AS total_lecturas,

                    COALESCE(
                        AVG(flujo_lpm),
                        0
                    ) AS flujo_promedio,

                    COALESCE(
                        AVG(presion_bar),
                        0
                    ) AS presion_promedio

                FROM lecturas

                WHERE `timestamp` >= @fechaInicio
                  AND `timestamp` < @fechaFinExclusiva
                  AND (
                      @filtrarSeccion = 0
                      OR seccion = @seccion
                  )
                """;

            await using var comando =
                new MySqlCommand(consulta, conexion)
                {
                    CommandTimeout = 15
                };

            AgregarParametros(
                comando,
                fechaInicio,
                fechaFinExclusiva,
                seccion
            );

            await using var reader =
                await comando.ExecuteReaderAsync(
                    cancellationToken
                );

            if (!await reader.ReadAsync(cancellationToken))
            {
                return;
            }

            resumen.TotalLecturas =
                LeerEntero(reader, "total_lecturas");

            resumen.FlujoPromedioLpm =
                Math.Round(
                    LeerDecimal(reader, "flujo_promedio"),
                    2
                );

            resumen.PresionPromedioBar =
                Math.Round(
                    LeerDecimal(reader, "presion_promedio"),
                    2
                );
        }

        private static async Task<List<EstadoValvulaInformeDto>>
            ObtenerEstadosValvulasAsync(
                MySqlConnection conexion,
                DateTime fechaInicio,
                DateTime fechaFinExclusiva,
                string seccion,
                CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT
                    l.seccion,
                    l.estado_valvula,
                    l.`timestamp`

                FROM lecturas l

                INNER JOIN
                (
                    SELECT
                        seccion,
                        MAX(id) AS ultima_lectura_id

                    FROM lecturas

                    WHERE `timestamp` >= @fechaInicio
                      AND `timestamp` < @fechaFinExclusiva
                      AND (
                          @filtrarSeccion = 0
                          OR seccion = @seccion
                      )

                    GROUP BY seccion
                ) ultimas

                    ON ultimas.ultima_lectura_id = l.id

                ORDER BY l.`timestamp` DESC
                """;

            List<EstadoValvulaInformeDto> valvulas =
                CrearValvulasBase(seccion);

            await using var comando =
                new MySqlCommand(consulta, conexion)
                {
                    CommandTimeout = 15
                };

            AgregarParametros(
                comando,
                fechaInicio,
                fechaFinExclusiva,
                seccion
            );

            await using var reader =
                await comando.ExecuteReaderAsync(
                    cancellationToken
                );

            var fechasPorValvula =
                new Dictionary<int, DateTime>();

            while (await reader.ReadAsync(cancellationToken))
            {
                string seccionLectura =
                    LeerTexto(reader, "seccion");

                int? numeroValvula =
                    ObtenerNumeroValvula(seccionLectura);

                if (numeroValvula is null)
                {
                    continue;
                }

                EstadoValvulaInformeDto? valvula =
                    valvulas.FirstOrDefault(
                        elemento =>
                            elemento.Numero == numeroValvula.Value
                    );

                if (valvula is null)
                {
                    continue;
                }

                DateTime fechaLectura =
                    LeerFechaComoDateTime(
                        reader,
                        "timestamp"
                    );

                if (
                    fechasPorValvula.TryGetValue(
                        numeroValvula.Value,
                        out DateTime fechaGuardada
                    )
                    && fechaGuardada >= fechaLectura
                )
                {
                    continue;
                }

                fechasPorValvula[numeroValvula.Value] =
                    fechaLectura;

                valvula.Estado =
                    NormalizarEstadoValvula(
                        LeerTexto(
                            reader,
                            "estado_valvula",
                            "Sin datos"
                        )
                    );

                valvula.UltimaSeccionReportada =
                    ObtenerEtiquetaSeccion(seccionLectura);

                valvula.FechaLectura =
                    fechaLectura == DateTime.MinValue
                        ? string.Empty
                        : fechaLectura.ToString(
                            "yyyy-MM-dd HH:mm:ss",
                            CultureInfo.InvariantCulture
                        );
            }

            return valvulas;
        }

        private static List<EstadoValvulaInformeDto>
            CrearValvulasBase(string seccion)
        {
            var valvulas = new List<EstadoValvulaInformeDto>
            {
                new EstadoValvulaInformeDto
                {
                    Numero = 1,
                    Nombre = "Válvula 1",
                    Secciones =
                        "Entrada y Tramo Derecho"
                },
                new EstadoValvulaInformeDto
                {
                    Numero = 2,
                    Nombre = "Válvula 2",
                    Secciones =
                        "Tramo Izquierdo y Parte Abajo"
                },
                new EstadoValvulaInformeDto
                {
                    Numero = 3,
                    Nombre = "Válvula 3",
                    Secciones = "Salida"
                }
            };

            if (seccion == "Todas")
            {
                return valvulas;
            }

            int? numeroValvula =
                ObtenerNumeroValvula(seccion);

            return numeroValvula is null
                ? []
                : valvulas
                    .Where(
                        valvula =>
                            valvula.Numero == numeroValvula.Value
                    )
                    .ToList();
        }

        private static int? ObtenerNumeroValvula(
            string seccion)
        {
            return seccion.Trim() switch
            {
                "Entrada" => 1,
                "Tramo_Derecho" => 1,
                "Tramo_Izquierdo" => 2,
                "Parte_Abajo" => 2,
                "Salida" => 3,
                _ => null
            };
        }

        private static string NormalizarEstadoValvula(
            string estado)
        {
            string estadoNormalizado =
                estado.Trim().ToLowerInvariant();

            if (
                estadoNormalizado.Contains("cerr")
                || estadoNormalizado == "0"
            )
            {
                return "Cerrada";
            }

            if (
                estadoNormalizado.Contains("abier")
                || estadoNormalizado.Contains("activ")
                || estadoNormalizado == "1"
            )
            {
                return "Abierta";
            }

            return "Sin datos";
        }

        private static void AgregarParametros(
            MySqlCommand comando,
            DateTime fechaInicio,
            DateTime fechaFinExclusiva,
            string seccion)
        {
            comando.Parameters.AddWithValue(
                "@fechaInicio",
                fechaInicio
            );

            comando.Parameters.AddWithValue(
                "@fechaFinExclusiva",
                fechaFinExclusiva
            );

            comando.Parameters.AddWithValue(
                "@filtrarSeccion",
                seccion == "Todas" ? 0 : 1
            );

            comando.Parameters.AddWithValue(
                "@seccion",
                seccion
            );
        }

        private static string NormalizarTipoInforme(
            string tipo)
        {
            return TiposInforme
                .FirstOrDefault(
                    opcion =>
                        opcion.Valor.Equals(
                            tipo.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                ?.Valor
                ?? string.Empty;
        }

        private static string NormalizarSeccion(
            string seccion)
        {
            return Secciones
                .FirstOrDefault(
                    opcion =>
                        opcion.Valor.Equals(
                            seccion.Trim(),
                            StringComparison.OrdinalIgnoreCase
                        )
                )
                ?.Valor
                ?? string.Empty;
        }

        private static string ObtenerEtiquetaTipo(
            string tipo)
        {
            return TiposInforme
                .First(
                    opcion => opcion.Valor == tipo
                )
                .Etiqueta;
        }

        private static string ObtenerEtiquetaSeccion(
            string seccion)
        {
            return Secciones
                .FirstOrDefault(
                    opcion => opcion.Valor == seccion
                )
                ?.Etiqueta
                ?? seccion.Replace("_", " ");
        }

        private static List<string>
            ObtenerSeccionesIncluidas(string seccion)
        {
            if (seccion != "Todas")
            {
                return
                [
                    ObtenerEtiquetaSeccion(seccion)
                ];
            }

            return Secciones
                .Where(
                    opcion => opcion.Valor != "Todas"
                )
                .Select(
                    opcion => opcion.Etiqueta
                )
                .ToList();
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

        private static decimal LeerDecimal(
            MySqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return 0;
            }

            return Convert.ToDecimal(
                reader.GetValue(posicion),
                CultureInfo.InvariantCulture
            );
        }

        private static string LeerTexto(
            MySqlDataReader reader,
            string columna,
            string valorPredeterminado = "")
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return valorPredeterminado;
            }

            return Convert.ToString(
                reader.GetValue(posicion),
                CultureInfo.InvariantCulture
            ) ?? valorPredeterminado;
        }

        private static DateTime LeerFechaComoDateTime(
            MySqlDataReader reader,
            string columna)
        {
            int posicion =
                reader.GetOrdinal(columna);

            if (reader.IsDBNull(posicion))
            {
                return DateTime.MinValue;
            }

            object valor =
                reader.GetValue(posicion);

            if (valor is DateTime fecha)
            {
                return fecha;
            }

            return DateTime.TryParse(
                Convert.ToString(
                    valor,
                    CultureInfo.InvariantCulture
                ),
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime fechaConvertida
            )
                ? fechaConvertida
                : DateTime.MinValue;
        }
    }
}