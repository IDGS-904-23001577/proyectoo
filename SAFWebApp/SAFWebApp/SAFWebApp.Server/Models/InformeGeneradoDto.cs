namespace SAFWebApp.Server.Models
{
    public class RegistrarInformeGeneradoDto
    {
        public string TipoInforme { get; set; } = string.Empty;

        public string FechaInicio { get; set; } = string.Empty;

        public string FechaFin { get; set; } = string.Empty;

        public string Seccion { get; set; } = string.Empty;

        public string NombreArchivo { get; set; } = string.Empty;

        public long TamanoBytes { get; set; }

        public int? UsuarioId { get; set; }
    }

    public class InformeGeneradoDto
    {
        public int Id { get; set; }

        public string TipoInforme { get; set; } = string.Empty;

        public string TipoInformeEtiqueta { get; set; } = string.Empty;

        public string FechaInicio { get; set; } = string.Empty;

        public string FechaFin { get; set; } = string.Empty;

        public string Seccion { get; set; } = string.Empty;

        public string SeccionEtiqueta { get; set; } = string.Empty;

        public string NombreArchivo { get; set; } = string.Empty;

        public long TamanoBytes { get; set; }

        public int? UsuarioId { get; set; }

        public string FechaGeneracion { get; set; } = string.Empty;
    }

    public class RegistroInformeRespuestaDto
    {
        public bool Ok { get; set; }

        public int Id { get; set; }
    }
}