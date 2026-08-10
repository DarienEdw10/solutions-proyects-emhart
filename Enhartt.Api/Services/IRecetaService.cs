using Enhartt.Domain.Models;

namespace Enhartt.Api.Services
{
    public interface IRecetaService
    {
        Task<IEnumerable<Receta>> ObtenerRecetaActivaAsync(int idMaquina, int salida);
        Task ActualizarRecetaConHistorialAsync(int idMaquina, int salida, List<Receta> nuevosParametros, string usuario);
        Task<IEnumerable<RecetaHistorialDto>> ObtenerHistorialCambiosAsync(int idMaquina, DateTime? fechaInicio, DateTime? fechaFin);
    }
}