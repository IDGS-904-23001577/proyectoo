using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly string _connectionString;

        public DashboardController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );
        }

        [HttpGet]
        public async Task<ActionResult<DashboardDto>> ObtenerDashboard(
            CancellationToken cancellationToken)
        {
            try
            {
                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                var respuesta = new DashboardDto
                {
                    FechaActualizacion = DateTimeOffset.UtcNow.ToString(
                        "O",
                        CultureInfo.InvariantCulture
                    )
                };

                respuesta.FugasActivas =
                    await ContarFugasActivas(conexion, cancellationToken);

                respuesta.AguaPerdidaHoyLitros =
                    await SumarAguaPerdidaHoy(conexion, cancellationToken);

                (respuesta.SeccionesOperativas, respuesta.SeccionesTotales) =
                    await ContarSecciones(conexion, cancellationToken);

                respuesta.ValvulasActivas =
                    await ContarValvulasActivas(conexion, cancellationToken);

                respuesta.EventosRecientes =
                    await ObtenerEventosRecientes(conexion, cancellationToken);

                respuesta.FugasSemanales =
                    await ObtenerFugasSemanales(conexion, cancellationToken);

                respuesta.PrediccionRiesgo =
                    await ObtenerTopRiesgo(conexion, cancellationToken);

                return Ok(respuesta);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error =
                            "No fue posible consultar el panel de control"
                    }
                );
            }
        }

        private static async Task<int> ContarFugasActivas(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta =
                "SELECT COUNT(*) FROM fugas WHERE estado = 'Activa'";

            await using var comando = new MySqlCommand(consulta, conexion);

            object? resultado =
                await comando.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(resultado);
        }

        private static async Task<double> SumarAguaPerdidaHoy(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT COALESCE(SUM(volumen_perdido_litros), 0)
                FROM lecturas
                WHERE DATE(timestamp) = CURDATE()
                """;

            await using var comando = new MySqlCommand(consulta, conexion);

            object? resultado =
                await comando.ExecuteScalarAsync(cancellationToken);

            return Convert.ToDouble(
                resultado,
                CultureInfo.InvariantCulture
            );
        }

        private static async Task<(int operativas, int totales)> ContarSecciones(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT
                    COUNT(DISTINCT seccion) AS total,
                    COUNT(DISTINCT CASE
                        WHEN seccion NOT IN (
                            SELECT seccion FROM fugas WHERE estado = 'Activa'
                        ) THEN seccion
                    END) AS operativas
                FROM lecturas
                """;

            await using var comando = new MySqlCommand(consulta, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            if (await reader.ReadAsync(cancellationToken))
            {
                int totales = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                int operativas = reader.IsDBNull(1) ? 0 : reader.GetInt32(1);

                return (operativas, totales);
            }

            return (0, 0);
        }

        private static async Task<int> ContarValvulasActivas(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT COUNT(*)
                FROM lecturas l
                INNER JOIN (
                    SELECT seccion, MAX(timestamp) AS ultima
                    FROM lecturas
                    GROUP BY seccion
                ) m ON l.seccion = m.seccion AND l.timestamp = m.ultima
                WHERE l.estado_valvula = 'Activa'
                """;

            await using var comando = new MySqlCommand(consulta, conexion);

            object? resultado =
                await comando.ExecuteScalarAsync(cancellationToken);

            return Convert.ToInt32(resultado);
        }

        private static async Task<List<EventoRecienteDto>> ObtenerEventosRecientes(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT seccion, severidad, fecha_deteccion
                FROM fugas
                ORDER BY fecha_deteccion DESC
                LIMIT 3
                """;

            var eventos = new List<EventoRecienteDto>();

            await using var comando = new MySqlCommand(consulta, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                eventos.Add(new EventoRecienteDto
                {
                    Seccion = LeerTexto(reader, "seccion"),
                    Severidad = LeerTexto(reader, "severidad", "Sin definir"),
                    FechaDeteccion = LeerFecha(reader, "fecha_deteccion")
                });
            }

            return eventos;
        }

        private static async Task<List<FugaSemanalDto>> ObtenerFugasSemanales(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT DATE(fecha_deteccion) AS dia, COUNT(*) AS cantidad
                FROM fugas
                WHERE fecha_deteccion >= CURDATE() - INTERVAL 6 DAY
                GROUP BY DATE(fecha_deteccion)
                """;

            var conteosPorFecha = new Dictionary<DateTime, int>();

            await using (var comando = new MySqlCommand(consulta, conexion))
            await using (var reader =
                await comando.ExecuteReaderAsync(cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    DateTime dia = reader.GetDateTime(0);
                    int cantidad = reader.GetInt32(1);

                    conteosPorFecha[dia.Date] = cantidad;
                }
            }

            string[] etiquetasDias =
                ["Dom", "Lun", "Mar", "Mié", "Jue", "Vie", "Sáb"];

            var resultado = new List<FugaSemanalDto>();

            for (int i = 6; i >= 0; i--)
            {
                DateTime dia = DateTime.UtcNow.Date.AddDays(-i);

                resultado.Add(new FugaSemanalDto
                {
                    Dia = etiquetasDias[(int)dia.DayOfWeek],
                    Cantidad = conteosPorFecha.GetValueOrDefault(dia, 0)
                });
            }

            return resultado;
        }

        private static async Task<List<RiesgoResumenDto>> ObtenerTopRiesgo(
            MySqlConnection conexion,
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT seccion, riesgo, pred_24h
                FROM predicciones_riesgo
                ORDER BY
                    CASE riesgo
                        WHEN 'ALTO' THEN 1
                        WHEN 'MEDIO' THEN 2
                        ELSE 3
                    END,
                    pred_24h DESC
                LIMIT 4
                """;

            var lista = new List<RiesgoResumenDto>();

            await using var comando = new MySqlCommand(consulta, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                lista.Add(new RiesgoResumenDto
                {
                    Seccion = LeerTexto(reader, "seccion"),
                    Riesgo = LeerTexto(reader, "riesgo", "Sin definir"),
                    PorcentajeMasReciente = reader.IsDBNull(
                        reader.GetOrdinal("pred_24h")
                    )
                        ? 0
                        : Convert.ToDouble(
                            reader.GetValue(reader.GetOrdinal("pred_24h")),
                            CultureInfo.InvariantCulture
                        )
                });
            }

            return lista;
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
