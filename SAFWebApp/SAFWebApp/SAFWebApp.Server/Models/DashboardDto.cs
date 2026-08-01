namespace SAFWebApp.Server.Models
{
    public class DashboardDto
    {
        public int FugasActivas { get; set; }

        public double AguaPerdidaHoyLitros { get; set; }

        public int SeccionesOperativas { get; set; }

        public int SeccionesTotales { get; set; }

        public int ValvulasActivas { get; set; }

        public List<EventoRecienteDto> EventosRecientes { get; set; } = [];

        public List<FugaSemanalDto> FugasSemanales { get; set; } = [];

        public List<RiesgoResumenDto> PrediccionRiesgo { get; set; } = [];

        public string FechaActualizacion { get; set; } = string.Empty;
    }

    public class EventoRecienteDto
    {
        public string Seccion { get; set; } = string.Empty;

        public string Severidad { get; set; } = string.Empty;

        public string FechaDeteccion { get; set; } = string.Empty;
    }

    public class FugaSemanalDto
    {
        public string Dia { get; set; } = string.Empty;

        public int Cantidad { get; set; }
    }

    public class RiesgoResumenDto
    {
        public string Seccion { get; set; } = string.Empty;

        public string Riesgo { get; set; } = string.Empty;

        public double PorcentajeMasReciente { get; set; }
    }
}
