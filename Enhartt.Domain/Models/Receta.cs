using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models
{
    [Table("receta", Schema = "emhart")]
    public class Receta
    {
        [Key]
        [Column("id_referencia")]
        public int IdReferencia { get; set; }

        [Column("id_maquina")]
        public int IdMaquina { get; set; }

        [Column("salida")]
        public int Salida { get; set; }

        [Column("parametro")]
        public string Parametro { get; set; } = string.Empty;

        [Column("min_val")]
        public double MinVal { get; set; }

        [Column("max_val")]
        public double MaxVal { get; set; }

        [Column("fecha_creacion")]
        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        [Column("fecha_modificacion")]
        public DateTime? FechaModificacion { get; set; }

        [Column("modificado_por")]
        public string ModificadoPor { get; set; } = "Usuario_Web";

        [Column("estado")]
        public bool Estado { get; set; } = true;
        // Campo nuevo para bitácora
        public string? Comentario { get; set; }
    }
}