namespace Enhartt.Domain.Models
{
    public class ResumenKpiDto
    {
        public int TotalDisparos { get; set; }
        public int CalidadOk { get; set; }
        public int DesviacionesNok { get; set; }
        public double EfectividadFtt { get; set; }
        public string UltimoDisparoCelda1 { get; set; } = "Sin datos";
        public string UltimoDisparoCelda3 { get; set; } = "Sin datos";
        public string EstatusCelda1 { get; set; } = "OPERANDO";
        public string EstatusCelda3 { get; set; } = "OPERANDO";
    }
}