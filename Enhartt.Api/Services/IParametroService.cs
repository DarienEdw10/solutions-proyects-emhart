using Enhartt.Domain.Models;

namespace Enhartt.Api.Services
{
    public interface IParametroService
    {
        Task<ResultadoPaginadoDto<object>> ObtenerParametrosPaginadosAsync(int pagina = 1, int registrosPorPagina = 5);
        Task<ResumenKpiDto> ObtenerResumenKpisAsync();
    }
}