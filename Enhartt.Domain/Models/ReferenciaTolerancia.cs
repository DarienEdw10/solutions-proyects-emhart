using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models
{
    [Table("referencia_tolerancia", Schema = "emhart")]
    public class ReferenciaTolerancia
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
        public DateTime FechaCreacion { get; set; }

        [Column("fecha_modificacion")]
        public DateTime? FechaModificacion { get; set; }

        [Column("modificado_por")]
        public string ModificadoPor { get; set; } = string.Empty;

        [Column("estado")]
        public bool Estado { get; set; }

        [ForeignKey("IdMaquina")]
        public Maquina? Maquina { get; set; }
    }
}