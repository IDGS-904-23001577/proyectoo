using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SAFWebApp.Server.Models;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AlertasController : ControllerBase
    {
        private readonly string _connectionString;

        public AlertasController(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "No se encontró la conexión DefaultConnection"
                );
        }

        [HttpGet]
        public async Task<ActionResult<List<AlertaDto>>> ObtenerAlertas(
            CancellationToken cancellationToken)
        {
            const string consulta = """
                SELECT
                    id,
                    seccion,
                    volumen_perdido,
                    fecha_deteccion,
                    duracion_horas,
                    severidad,
                    estado
                FROM fugas
                ORDER BY fecha_deteccion DESC
                """;

            try
            {
                var alertas = new List<AlertaDto>();

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
                    alertas.Add(new AlertaDto
                    {
                        Id = LeerTexto(reader, "id"),
                        Seccion = LeerTexto(reader, "seccion"),
                        VolumenPerdido =
                            LeerTexto(reader, "volumen_perdido", "0"),
                        FechaDeteccion =
                            LeerFecha(reader, "fecha_deteccion"),
                        DuracionHoras =
                            LeerTexto(reader, "duracion_horas", "0"),
                        Severidad =
                            LeerTexto(reader, "severidad", "Sin definir"),
                        Estado =
                            LeerTexto(reader, "estado", "Pendiente")
                    });
                }

                return Ok(alertas);
            }
            catch (MySqlException)
            {
                return StatusCode(
                    StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error =
                            "No fue posible consultar las alertas"
                    }
                );
            }
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