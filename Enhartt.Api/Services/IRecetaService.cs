using Enhartt.Domain.Models;

namespace Enhartt.Api.Services
{
    public interface IRecetaService
    {
        Task GuardarCambiosRecetaAsync(string celda, string salida, List<HistorialReceta> cambios);
        Task<IEnumerable<HistorialComparativoDto>> ObtenerHistorialAsync(string? celda, DateTime? fechaInicio, DateTime? fechaFin);
    }
}