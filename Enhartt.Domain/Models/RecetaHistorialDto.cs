namespace Enhartt.Domain.Models
{
    public class RecetaHistorialDto
    {
        public int IdReferencia { get; set; }
        public string Celda { get; set; } = string.Empty;
        public int Salida { get; set; }
        public string Parametro { get; set; } = string.Empty;
        public double MinVal { get; set; }
        public double MaxVal { get; set; }
        public string FechaRegistro { get; set; } = string.Empty;
        public string ModificadoPor { get; set; } = string.Empty;
        public string EstatusRegistro { get; set; } = string.Empty; // "ACTIVO (ACTUAL)" o "INHABILITADO (ANTERIOR)"
    }
}