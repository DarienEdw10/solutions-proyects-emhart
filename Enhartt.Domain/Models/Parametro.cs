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

        // Cambiados de double? a int? para coincidir con el [int] NULL de SQL Server
        [Column("VolArc")]
        public int? VolArc { get; set; }

        [Column("VolPri")]
        public int? VolPri { get; set; }

        [Column("Salida")]
        public int? Salida { get; set; }

        [Column("Programa")]
        public int? Programa { get; set; }

        [Column("Elevacion")]
        public int? Elevacion { get; set; }

        [Column("Caida")]
        public int? Caida { get; set; }

        [Column("Penetracion")]
        public int? Penetracion { get; set; }

        [Column("Energia")]
        public int? Energia { get; set; }

        [Column("Corriente")]
        public int? Corriente { get; set; }

        [Column("Tiempo")]
        public int? Tiempo { get; set; }

        [Column("LonPer")]
        public int? LonPer { get; set; }

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