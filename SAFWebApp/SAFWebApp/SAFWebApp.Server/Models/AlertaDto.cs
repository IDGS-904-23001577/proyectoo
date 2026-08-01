namespace SAFWebApp.Server.Models
{
    public class AlertaDto
    {
        public string Id { get; set; } = string.Empty;

        public string Seccion { get; set; } = string.Empty;

        public string VolumenPerdido { get; set; } = "0";

        public string FechaDeteccion { get; set; } = string.Empty;

        public string DuracionHoras { get; set; } = "0";

        public string Severidad { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;
    }
}