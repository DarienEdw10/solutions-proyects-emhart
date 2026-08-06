using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models
{
    [Table("maquinas", Schema = "emhart")]
    public class Maquina
    {
        [Key]
        [Column("Id")]
        public int Id { get; set; }

        [Column("Planta")]
        public string? Planta { get; set; }

        [Column("Linea")]
        public string? Linea { get; set; }

        [Column("Celda")]
        public string? Celda { get; set; }

        [Column("Estacion")]
        public string? Estacion { get; set; }

        [Column("IdMaquina")]
        public string IdMaquina { get; set; } = string.Empty;

        [Column("Modelo")]
        public string? Modelo { get; set; }

        [Column("FechaCreacion")]
        public DateTime? FechaCreacion { get; set; }

        [Column("FechaModificacion")]
        public DateTime? FechaModificacion { get; set; }

        [Column("Activo")]
        public int? Activo { get; set; }
    }
}