namespace SAFWebApp.Server.Models
{
    public class PrediccionDto
    {
        public string Seccion { get; set; } = string.Empty;

        public string Etiqueta { get; set; } = string.Empty;

        public string Riesgo { get; set; } = string.Empty;

        public double Pred24h { get; set; }

        public double Pred48h { get; set; }

        public double Pred72h { get; set; }

        public double PorcentajeRelativo { get; set; }

        public string FechaCalculo { get; set; } = string.Empty;
    }

    public class PrediccionHistorialPuntoDto
    {
        public string Fecha { get; set; } = string.Empty;

        public double PresionPromedio { get; set; }

        public double CaudalPromedio { get; set; }
    }
}