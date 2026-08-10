using System.Linq.Expressions;
using Enhartt.Domain.Models;

namespace Enhartt.Domain.Repositories
{
    public interface IRepository 
    {
        // -------------------------------------------------------------
        // MÓDULO RECETAS (Tolerancias)
        // -------------------------------------------------------------
        Task<IEnumerable<Receta>> ObtenerRecetasPorMaquinaAsync(int idMaquina);
        Task<Receta?> ActualizarRecetaAsync(Receta receta, string usuario);
        Task<Receta?> AgregarRecetaAsync(Receta receta, string usuario);

        // -------------------------------------------------------------
        // MÓDULO MAQUINAS (Catálogo Celdas Tucker)
        // -------------------------------------------------------------
        Task<IEnumerable<Maquina>> ObtenerMaquinasActivasAsync();
        Task<Maquina?> ObtenerMaquinaPorIdAsync(string idMaquina);
        Task<Maquina?> AgregarMaquinaAsync(Maquina maquina);
        Task<Maquina?> ActualizarMaquinaAsync(Maquina maquina);

        // -------------------------------------------------------------
        // MÓDULO PARAMETROS (Telemetría Disparos / Ingesta PLC)
        // -------------------------------------------------------------
        Task<IEnumerable<Parametro>> ObtenerParametrosPaginadosAsync(
            int? identificadorId, 
            string? estatus, 
            DateTime? fechaInicio, 
            DateTime? fechaFin, 
            string? busqueda, 
            int pagina, 
            int registrosPorPagina);

        Task<int> ContarParametrosTotalAsync(
            int? identificadorId, 
            string? estatus, 
            DateTime? fechaInicio, 
            DateTime? fechaFin, 
            string? busqueda);

        Task<IEnumerable<int>> ObtenerSalidasPorMaquinaAsync(int idMaquina);
        Task<Parametro?> AgregarParametroIngestaAsync(Parametro parametro);
    }
}