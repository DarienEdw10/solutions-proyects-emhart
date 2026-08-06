using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models
{
    [Table("parametros", Schema = "emhart")]
    public class Parametro
    {
        [Key]
        [Column("Identificador_ID")]
        public int IdentificadorId { get; set; }

        public DateTime? Fecha { get; set; }
        public int? Salida { get; set; }
        public int? Programa { get; set; }

        [Column("num_sol")]
        public int? NumSol { get; set; }

        public double? Corriente { get; set; }
        public double? Energia { get; set; }
        public double? Tiempo { get; set; }
        public double? Penetracion { get; set; }

        [Column("vol_arc")]
        public double? VolArc { get; set; }

        [Column("vol_pri")]
        public double? VolPri { get; set; }

        public double? Elevacion { get; set; }
        public double? Caida { get; set; }

        [Column("lon_per")]
        public double? LonPer { get; set; }

        [Column("estatus_calidad")]
        public string? EstatusCalidad { get; set; }

        [Column("detalles_fallas")]
        public string? DetallesFallas { get; set; }
    }
}