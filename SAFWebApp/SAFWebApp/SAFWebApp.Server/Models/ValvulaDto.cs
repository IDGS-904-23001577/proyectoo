namespace SAFWebApp.Server.Models;

public class ValvulaDto
{
    public int Numero { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Secciones { get; set; } = string.Empty;

    public string Topic { get; set; } = string.Empty;

    public string Estado { get; set; } = "Abierta";

    public string UltimaActualizacion { get; set; } = string.Empty;

    public bool Disponible { get; set; } = true;
}

public class ComandoValvulaDto
{
    public string Estado { get; set; } = string.Empty;
}

public class RespuestaComandoValvulaDto
{
    public bool Ok { get; set; }

    public string Mensaje { get; set; } = string.Empty;

    public ValvulaDto Valvula { get; set; } = new();
}