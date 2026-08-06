namespace Enhartt.Domain.Models
{
    public class HistorialComparativoDto
    {
        public int IdHistorial { get; set; }
        public string Celda { get; set; } = string.Empty;
        public string Salida { get; set; } = string.Empty;
        public string Parametro { get; set; } = string.Empty;
        public string ValorAnterior { get; set; } = string.Empty;
        public string ValorNuevo { get; set; } = string.Empty;
        public string Fecha { get; set; } = string.Empty;
        public string Usuario { get; set; } = string.Empty;
    }
}