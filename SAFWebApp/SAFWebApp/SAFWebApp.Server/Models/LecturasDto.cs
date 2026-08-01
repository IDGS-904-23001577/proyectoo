namespace SAFWebApp.Server.Models
{
    public class LecturaActualDto
    {
        public string Seccion { get; set; } = string.Empty;

        public string Etiqueta { get; set; } = string.Empty;

        public double PresionBar { get; set; }

        public double CaudalLmin { get; set; }

        public string EstadoValvula { get; set; } = string.Empty;

        public string Estado { get; set; } = "Normal";

        public bool SensorEnLinea { get; set; }

        public string UltimaLectura { get; set; } = string.Empty;
    }

    public class LecturaHistorialPuntoDto
    {
        public string Timestamp { get; set; } = string.Empty;

        public double PresionBar { get; set; }

        public double CaudalLmin { get; set; }
    }
}