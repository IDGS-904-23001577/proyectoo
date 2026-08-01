namespace SAFWebApp.Server.Models
{
    public class CatalogoInformesDto
    {
        public List<OpcionCatalogoDto> TiposInforme { get; set; } = [];

        public List<OpcionCatalogoDto> Secciones { get; set; } = [];
    }

    public class OpcionCatalogoDto
    {
        public string Valor { get; set; } = string.Empty;

        public string Etiqueta { get; set; } = string.Empty;
    }

    public class VistaPreviaInformeDto
    {
        public string TipoInforme { get; set; } = string.Empty;

        public string TipoInformeEtiqueta { get; set; } = string.Empty;

        public string FechaInicio { get; set; } = string.Empty;

        public string FechaFin { get; set; } = string.Empty;

        public string Seccion { get; set; } = string.Empty;

        public string SeccionEtiqueta { get; set; } = string.Empty;

        public ResumenInformeDto Resumen { get; set; } = new();

        public List<string> SeccionesIncluidas { get; set; } = [];

        public List<EstadoValvulaInformeDto> Valvulas { get; set; } = [];

        public string FechaGeneracion { get; set; } = string.Empty;
    }

    public class ResumenInformeDto
    {
        public int TotalFugas { get; set; }

        public int FugasActivas { get; set; }

        public int FugasPendientes { get; set; }

        public int FugasResueltas { get; set; }

        public decimal VolumenPerdidoLitros { get; set; }

        public int SeccionesAfectadas { get; set; }

        public int TotalIntervenciones { get; set; }

        public int TotalLecturas { get; set; }

        public decimal FlujoPromedioLpm { get; set; }

        public decimal PresionPromedioBar { get; set; }

        public int ValvulasAbiertas { get; set; }

        public int ValvulasCerradas { get; set; }
    }

    public class EstadoValvulaInformeDto
    {
        public int Numero { get; set; }

        public string Nombre { get; set; } = string.Empty;

        public string Secciones { get; set; } = string.Empty;

        public string Estado { get; set; } = "Sin datos";

        public string UltimaSeccionReportada { get; set; } = string.Empty;

        public string FechaLectura { get; set; } = string.Empty;
    }
}