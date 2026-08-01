namespace SAFWebApp.Server.Models
{
    public class RedDistribucionDto
    {
        public List<NodoRedDto> Nodos { get; set; } = [];

        public List<ConexionRedDto> Conexiones { get; set; } = [];

        public string FechaActualizacion { get; set; } = string.Empty;
    }

    public class NodoRedDto
    {
        public string Id { get; set; } = string.Empty;

        public string Etiqueta { get; set; } = string.Empty;

        public int PosicionX { get; set; }

        public int PosicionY { get; set; }

        public string Estado { get; set; } = "Normal";

        public string EstadoFuga { get; set; } = string.Empty;

        public string Severidad { get; set; } = string.Empty;

        public string? FugaId { get; set; }

        public string SeccionAfectada { get; set; } = string.Empty;

        public string FechaDeteccion { get; set; } = string.Empty;
    }

    public class ConexionRedDto
    {
        public string Origen { get; set; } = string.Empty;

        public string Destino { get; set; } = string.Empty;
    }
}