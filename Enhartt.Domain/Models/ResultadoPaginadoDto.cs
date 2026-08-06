namespace Enhartt.Domain.Models
{
    public class ResultadoPaginadoDto<T>
    {
        public IEnumerable<T> Elementos { get; set; } = new List<T>();
        public int TotalRegistros { get; set; }
        public int PaginaActual { get; set; }
        public int RegistrosPorPagina { get; set; }
        public int TotalPaginas => (int)Math.Ceiling((double)TotalRegistros / RegistrosPorPagina);
    }
}