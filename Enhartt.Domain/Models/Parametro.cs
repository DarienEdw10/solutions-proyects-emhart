using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models
{
    [Table("parametros", Schema = "emhart")]
    public class Parametro
    {
        [Key]
        [Column("id_registro")]
        public int IdRegistro { get; set; }

        [Column("Fecha")]
        public DateTime? Fecha { get; set; }

        [Column("Linea")]
        public string? Linea { get; set; }

        [Column("Identificador_ID")]
        public int? IdentificadorId { get; set; }

        [Column("NumSol")]
        public int? NumSol { get; set; }

        [Column("VolArc")]
        public double? VolArc { get; set; }

        [Column("VolPri")]
        public double? VolPri { get; set; }

        [Column("Salida")]
        public int? Salida { get; set; }

        [Column("Programa")]
        public int? Programa { get; set; }

        [Column("Elevacion")]
        public double? Elevacion { get; set; }

        [Column("Caida")]
        public double? Caida { get; set; }

        [Column("Penetracion")]
        public double? Penetracion { get; set; }

        [Column("Energia")]
        public double? Energia { get; set; }

        [Column("Corriente")]
        public double? Corriente { get; set; }

        [Column("Tiempo")]
        public double? Tiempo { get; set; }

        [Column("LonPer")]
        public double? LonPer { get; set; }

        [Column("fecha_creacion")]
        public DateTime? FechaCreacion { get; set; }

        [Column("estatus_calidad")]
        public string? EstatusCalidad { get; set; }

        [Column("detalles_fallas")]
        public string? DetallesFallas { get; set; }

        [ForeignKey("IdentificadorId")]
        public Maquina? Maquina { get; set; }
    }
}