using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PrediccionesController : ControllerBase
    {
        private static readonly string[] SeccionesValidas =
        [
            "Entrada",
            "Tramo_Izquierdo",
            "Tramo_Derecho",
            "Parte_Abajo",
            "Salida"
        ];

        private static readonly int[] DiasPermitidos = [7, 30, 90];

        private readonly string _connectionString;

        public PrediccionesController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );
        }

        [HttpGet]
        public async Task<ActionResult<List<PrediccionDto>>> ObtenerPredicciones(
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT p.seccion, p.riesgo, p.pred_24h, p.pred_48h,
                       p.pred_72h, p.fecha_calculo
                FROM predicciones_riesgo p
                INNER JOIN (
                    SELECT seccion, MAX(fecha_calculo) AS ultima
                    FROM predicciones_riesgo
                    GROUP BY seccion
                ) m ON p.seccion = m.seccion
                    AND p.fecha_calculo = m.ultima
                """;

            try
            {
                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                var resultado = new List<PrediccionDto>();

                await using var comando =
                    new MySqlCommand(consulta, conexion)
                    {
                        CommandTimeout = 15
                    };

                await using var reader =
                    await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    string seccion = LeerTexto(reader, "seccion");

                    resultado.Add(new PrediccionDto
                    {
                        Seccion = seccion,
                        Etiqueta = seccion.Replace('_', ' '),
                        Riesgo = LeerTexto(
                            reader,
                            "riesgo",
                            "Sin definir"
                        ),
                        Pred24h = LeerDouble(reader, "pred_24h"),
                        Pred48h = LeerDouble(reader, "pred_48h"),
                        Pred72h = LeerDouble(reader, "pred_72h"),
                        FechaCalculo = LeerFecha(reader, "fecha_calculo")
                    });
                }

                AsignarPorcentajeRelativo(resultado);

                List<PrediccionDto> ordenado = resultado
                    .OrderBy(item => OrdenRiesgo(item.Riesgo))
                    .ThenByDescending(item => item.PorcentajeRelativo)
                    .ToList();

                return Ok(ordenado);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { error = "No fue posible consultar las predicciones" }
                );
            }
        }

        [HttpGet("historial")]
        public async Task<ActionResult<List<PrediccionHistorialPuntoDto>>>
            ObtenerHistorial(
                [FromQuery] string seccion,
                [FromQuery] int dias,
                CancellationToken cancellationToken)
        {
            if (!SeccionesValidas.Contains(seccion))
            {
                return BadRequest(new { error = "Sección inválida" });
            }

            int diasConsulta = DiasPermitidos.Contains(dias) ? dias : 30;

            const string consulta = """
                SELECT
                    DATE(timestamp) AS fecha,
                    AVG(presion_bar) AS presion_promedio,
                    AVG(flujo_lpm) AS caudal_promedio
                FROM lecturas
                WHERE seccion = @seccion
                    AND timestamp >= CURDATE() - INTERVAL @dias DAY
                GROUP BY DATE(timestamp)
                ORDER BY fecha ASC
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

                comando.Parameters.AddWithValue("@seccion", seccion);
                comando.Parameters.AddWithValue("@dias", diasConsulta);

                var resultado = new List<PrediccionHistorialPuntoDto>();

                await using var reader =
                    await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    resultado.Add(new PrediccionHistorialPuntoDto
                    {
                        Fecha = LeerFecha(reader, "fecha"),
                        PresionPromedio =
                            LeerDouble(reader, "presion_promedio"),
                        CaudalPromedio =
                            LeerDouble(reader, "caudal_promedio")
                    });
                }

                return Ok(resultado);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { error = "No fue posible consultar el histórico" }
                );
            }
        }

        /// <summary>
        /// No existe una columna real de probabilidad en la tabla, así que
        /// esto normaliza pred_24h entre las secciones devueltas (0-100%)
        /// solo como referencia visual, no como una probabilidad estadística.
        /// </summary>
        private static void AsignarPorcentajeRelativo(
            List<PrediccionDto> predicciones)
        {
            if (predicciones.Count == 0)
            {
                return;
            }

            double minimo = predicciones.Min(item => item.Pred24h);
            double maximo = predicciones.Max(item => item.Pred24h);
            double rango = maximo - minimo;

            foreach (PrediccionDto prediccion in predicciones)
            {
                prediccion.PorcentajeRelativo = rango == 0
                    ? 50
                    : (prediccion.Pred24h - minimo) / rango * 100;
            }
        }

        private static int OrdenRiesgo(string riesgo)
        {
            return riesgo.Trim().ToUpperInvariant() switch
            {
                "ALTO" => 1,
                "MEDIO" => 2,
                "BAJO" => 3,
                _ => 4
            };
        }

        private static double LeerDouble(
            MySqlDataReader reader,
            string columna)
        {
            int posicion = reader.GetOrdinal(columna);

            return reader.IsDBNull(posicion)
                ? 0
                : reader.GetDouble(posicion);
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
                    "yyyy-MM-dd",
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