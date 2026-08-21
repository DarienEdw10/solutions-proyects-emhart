using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Enhartt.Domain.Models;

[Table("usuarios", Schema = "emhart")]
public class Usuario
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string CWID { get; set; } = string.Empty;

    public int NivelDeUsuario { get; set; } = 10;

    [Required]
    [MaxLength(100)]
    public string Descripcion { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}