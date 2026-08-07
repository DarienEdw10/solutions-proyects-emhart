using Enhartt.Domain.Models;

namespace Enhartt.Api.Services
{
    public interface IParametroService
    {
        Task<ResultadoPaginadoDto<object>> ObtenerParametrosPaginadosAsync(
            int pagina = 1, 
            int registrosPorPagina = 5,
            string? celda = null,
            string? estatus = null,
            DateTime? fechaInicio = null,
            DateTime? fechaFin = null,
            string? busqueda = null);

        Task<ResumenKpiDto> ObtenerResumenKpisAsync();
    }
}