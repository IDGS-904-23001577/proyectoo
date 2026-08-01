using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LecturasController : ControllerBase
    {
        private static readonly string[] SeccionesValidas =
        [
            "Entrada",
            "Tramo_Izquierdo",
            "Tramo_Derecho",
            "Parte_Abajo",
            "Salida"
        ];

        private readonly string _connectionString;

        public LecturasController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );
        }

        [HttpGet("actual")]
        public async Task<ActionResult<List<LecturaActualDto>>> ObtenerActual(
            CancellationToken cancellationToken)
        {
            const string consultaLecturas = """
                SELECT s.seccion, l.presion_bar, l.flujo_lpm,
                       l.estado_valvula, l.timestamp,
                       TIMESTAMPDIFF(SECOND, l.timestamp, NOW())
                           AS segundos_desde_lectura
                FROM (
                    SELECT 'Entrada' AS seccion
                    UNION ALL SELECT 'Tramo_Izquierdo'
                    UNION ALL SELECT 'Tramo_Derecho'
                    UNION ALL SELECT 'Parte_Abajo'
                    UNION ALL SELECT 'Salida'
                ) s
                LEFT JOIN (
                    SELECT l1.*
                    FROM lecturas l1
                    INNER JOIN (
                        SELECT seccion, MAX(timestamp) AS ultima
                        FROM lecturas
                        GROUP BY seccion
                    ) m ON l1.seccion = m.seccion
                        AND l1.timestamp = m.ultima
                ) l ON l.seccion = s.seccion
                """;

            const string consultaFugas = """
                SELECT seccion, estado
                FROM fugas
                WHERE estado IN ('Activa', 'Pendiente')
                """;

            try
            {
                await using var conexion =
                    new MySqlConnection(_connectionString);

                await conexion.OpenAsync(cancellationToken);

                var estadosFugaPorSeccion =
                    await ObtenerEstadosFugaPorSeccion(
                        conexion,
                        consultaFugas,
                        cancellationToken
                    );

                var resultado = new List<LecturaActualDto>();

                await using var comando =
                    new MySqlCommand(consultaLecturas, conexion)
                    {
                        CommandTimeout = 15
                    };

                await using var reader =
                    await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    string seccion = LeerTexto(reader, "seccion");

                    var dto = new LecturaActualDto
                    {
                        Seccion = seccion,
                        Etiqueta = seccion.Replace('_', ' '),
                        EstadoValvula = LeerTexto(
                            reader,
                            "estado_valvula",
                            "Sin datos"
                        ),
                        UltimaLectura = LeerFecha(reader, "timestamp")
                    };

                    int posPresion = reader.GetOrdinal("presion_bar");
                    int posCaudal = reader.GetOrdinal("flujo_lpm");
                    int posSegundos =
                        reader.GetOrdinal("segundos_desde_lectura");

                    dto.PresionBar = reader.IsDBNull(posPresion)
                        ? 0
                        : reader.GetDouble(posPresion);

                    dto.CaudalLmin = reader.IsDBNull(posCaudal)
                        ? 0
                        : reader.GetDouble(posCaudal);

                    dto.SensorEnLinea = !reader.IsDBNull(posSegundos)
                        && reader.GetInt32(posSegundos) <= 300;

                    dto.Estado = estadosFugaPorSeccion.TryGetValue(
                        seccion,
                        out string? estadoFuga
                    )
                        ? ObtenerEstadoVisual(estadoFuga)
                        : "Normal";

                    resultado.Add(dto);
                }

                return Ok(resultado);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { error = "No fue posible consultar las lecturas" }
                );
            }
        }

        [HttpGet("historial")]
        public async Task<ActionResult<List<LecturaHistorialPuntoDto>>>
            ObtenerHistorial(
                [FromQuery] string seccion,
                [FromQuery] int minutos,
                CancellationToken cancellationToken)
        {
            if (!SeccionesValidas.Contains(seccion))
            {
                return BadRequest(new { error = "Sección inválida" });
            }

            int minutosConsulta = minutos <= 0
                ? 15
                : Math.Min(minutos, 1440);

            const string consulta = """
                SELECT timestamp, presion_bar, flujo_lpm
                FROM lecturas
                WHERE seccion = @seccion
                    AND timestamp >= NOW() - INTERVAL @minutos MINUTE
                ORDER BY timestamp ASC
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
                comando.Parameters.AddWithValue(
                    "@minutos",
                    minutosConsulta
                );

                var resultado = new List<LecturaHistorialPuntoDto>();

                await using var reader =
                    await comando.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    resultado.Add(new LecturaHistorialPuntoDto
                    {
                        Timestamp = LeerFecha(reader, "timestamp"),
                        PresionBar = reader.IsDBNull(
                            reader.GetOrdinal("presion_bar")
                        )
                            ? 0
                            : reader.GetDouble(
                                reader.GetOrdinal("presion_bar")
                            ),
                        CaudalLmin = reader.IsDBNull(
                            reader.GetOrdinal("flujo_lpm")
                        )
                            ? 0
                            : reader.GetDouble(
                                reader.GetOrdinal("flujo_lpm")
                            )
                    });
                }

                return Ok(resultado);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new { error = "No fue posible consultar el historial" }
                );
            }
        }

        private static async Task<Dictionary<string, string>>
            ObtenerEstadosFugaPorSeccion(
                MySqlConnection conexion,
                string consultaFugas,
                CancellationToken cancellationToken)
        {
            var estados = new Dictionary<string, string>();

            await using var comando =
                new MySqlCommand(consultaFugas, conexion);

            await using var reader =
                await comando.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                string seccion = LeerTexto(reader, "seccion");
                string estado = LeerTexto(reader, "estado");

                // Si ya hay una fuga "Activa" registrada para la sección,
                // esa domina sobre una "Pendiente" que llegue después.
                if (estados.TryGetValue(seccion, out string? actual)
                    && actual.Equals(
                        "Activa",
                        StringComparison.OrdinalIgnoreCase
                    ))
                {
                    continue;
                }

                estados[seccion] = estado;
            }

            return estados;
        }

        private static string ObtenerEstadoVisual(string estadoFuga)
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