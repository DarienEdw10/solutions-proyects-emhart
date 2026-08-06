using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models
{
    [Table("historial_recetas", Schema = "emhart")]
    public class HistorialReceta
    {
        [Key]
        public int IdHistorial { get; set; }
        public string Celda { get; set; } = string.Empty;
        public string Salida { get; set; } = string.Empty;
        public string Parametro { get; set; } = string.Empty;
        public double MinAnterior { get; set; }
        public double MaxAnterior { get; set; }
        public double MinNuevo { get; set; }
        public double MaxNuevo { get; set; }
        public DateTime FechaModificacion { get; set; } = DateTime.Now;
        public string Usuario { get; set; } = "Usuario_Web";
    }
}