using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace SAFWebApp.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly string _connectionString;

        public AuthController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest datos, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(datos.Correo) || string.IsNullOrEmpty(datos.Password))
            {
                return BadRequest(new { error = "Correo y password requeridos" });
            }

            try
            {
                using var conexion = new MySqlConnection(_connectionString);
                await conexion.OpenAsync(cancellationToken);

                const string query = "SELECT id, nombre, correo, rol, empleado_id FROM usuarios WHERE correo = @correo AND password = @password";
                using var comando = new MySqlCommand(query, conexion)
                {
                    CommandTimeout = 10
                };
                comando.Parameters.AddWithValue("@correo", datos.Correo.Trim());
                comando.Parameters.AddWithValue("@password", datos.Password);

                using var reader = await comando.ExecuteReaderAsync(cancellationToken);
                if (await reader.ReadAsync(cancellationToken))
                {
                    return Ok(new
                    {
                        ok = true,

                        id = reader.IsDBNull(reader.GetOrdinal("id"))
        ? null
        : reader["id"].ToString(),

                        nombre = reader.IsDBNull(reader.GetOrdinal("nombre"))
        ? ""
        : reader["nombre"].ToString(),

                        correo = reader.IsDBNull(reader.GetOrdinal("correo"))
        ? ""
        : reader["correo"].ToString(),

                        rol = reader.IsDBNull(reader.GetOrdinal("rol"))
        ? ""
        : reader["rol"].ToString(),

                        empleado_id = reader.IsDBNull(reader.GetOrdinal("empleado_id"))
        ? null
        : reader["empleado_id"].ToString()
                    });
                }

                return Unauthorized(new { error = "Correo o password incorrectos" });
            }
            catch (MySqlException ex)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new
                    {
                        error = ex.Message,
                        codigo = ex.Number,
                        detalle = ex.InnerException?.Message
                    });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    error = ex.Message,
                    detalle = ex.InnerException?.Message
                });
            }
        }
    }

    public class LoginRequest
    {
        public string Correo { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
