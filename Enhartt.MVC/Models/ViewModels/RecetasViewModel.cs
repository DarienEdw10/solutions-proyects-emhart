namespace Enhartt.MVC.Models.ViewModels
{
    public class RecetasViewModel
    {
        public List<CeldaViewModel> Celdas { get; set; } = [];
    }

    public class CeldaViewModel
    {
        public string Celda { get; set; } = string.Empty;
        public List<MaquinaViewModel> Maquinas { get; set; } = [];
    }

    public class MaquinaViewModel
    {
        public int Id { get; set; }
        public string IdMaquina { get; set; } = string.Empty;
        public List<RecetaViewModel> Recetas { get; set; } = [];
    }

    public class RecetaViewModel
    {
        public int Id { get; set; }
        public string Salida { get; set; } = string.Empty;
        public string Parametro { get; set; } = string.Empty; // VolArc, Corriente, Tiempo, etc.
        public double MinVal { get; set; }
        public double MaxVal { get; set; }
        public DateTime? FechaModificacion { get; set; }
        public string ModificadoPor { get; set; } = string.Empty;
        public bool Estado { get; set; }
    }
}