namespace SAFWebApp.Server.Models;

public class EstadoApiDto
{
    public string Estado { get; set; } = "Operativa";

    public string Version { get; set; } = "1.0.0";

    public string Ambiente { get; set; } = string.Empty;

    public bool BaseDatos { get; set; }

    public string FechaServidor { get; set; } = string.Empty;

    public long TiempoRespuestaMs { get; set; }

    public List<EndpointApiDto> Endpoints { get; set; } = [];
}

public class EndpointApiDto
{
    public string Metodo { get; set; } = string.Empty;

    public string Ruta { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Modulo { get; set; } = string.Empty;
}