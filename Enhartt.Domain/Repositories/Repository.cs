using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Data;
using Enhartt.Domain.Models;

namespace Enhartt.Domain.Repositories
{
    public class Repository : IRepository
    {
        protected readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        // =============================================================
        // RECETAS
        // =============================================================
        public async Task<IEnumerable<Receta>> ObtenerRecetasPorMaquinaAsync(int idMaquina)
        {
            return await _context.Receta
                                 .AsNoTracking()
                                 .Where(receta => receta.IdMaquina == idMaquina)
                                 .ToListAsync();
        }

        public async Task<Receta?> ActualizarRecetaAsync(Receta receta, string usuario)
        {
            Receta? recetaBd = await _context.Receta.FirstOrDefaultAsync(receta => receta.IdReferencia == receta.IdReferencia);

            if (recetaBd == null) return null;

            recetaBd.MinVal = receta.MinVal;
            recetaBd.MaxVal = receta.MaxVal;
            recetaBd.Estado = receta.Estado;
            recetaBd.ModificadoPor = usuario;

            var entityEntry = _context.Receta.Update(recetaBd);
            return entityEntry.Entity;
        }

        public async Task<Receta?> AgregarRecetaAsync(Receta receta, string usuario)
        {
            receta.Estado = true;
            var entityEntry = await _context.Receta.AddAsync(receta);
            return entityEntry.Entity;
        }

        // =============================================================
        // MAQUINAS
        // =============================================================
        public async Task<IEnumerable<Maquina>> ObtenerMaquinasActivasAsync()
        {
            return await _context.Maquinas
                                 .AsNoTracking()
                                 .Where(Maquina => Maquina.Activo == true)
                                 .ToListAsync();
        }

        public async Task<Maquina?> ObtenerMaquinaPorIdAsync(string idMaquina)
        {
            return await _context.Maquinas
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(m => m.IdMaquina == idMaquina);
        }

        // =============================================================
        // PARAMETROS (Telemetría de Soldaduras)
        // =============================================================
        private IQueryable<Parametro> ConstruirFiltroParametros(
            int? identificadorId, 
            string? estatus, 
            DateTime? fechaInicio, 
            DateTime? fechaFin, 
            string? busqueda)
        {
            var query = _context.Parametros.AsNoTracking().AsQueryable();

            if (identificadorId.HasValue)
                query = query.Where(Parametro => Parametro.IdentificadorId == identificadorId.Value);

            if (!string.IsNullOrEmpty(estatus))
                query = query.Where(Parametro => Parametro.EstatusCalidad == estatus);

            if (fechaInicio.HasValue)
                query = query.Where(Parametro => Parametro.Fecha >= fechaInicio.Value.Date);

            if (fechaFin.HasValue)
                query = query.Where(Parametro => Parametro.Fecha <= fechaFin.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(Parametro => (Parametro.NumSol.HasValue && EF.Functions.Like(Parametro.NumSol.Value.ToString(), $"%{busqueda}%")) ||
                                         (Parametro.DetallesFallas != null && Parametro.DetallesFallas.Contains(busqueda)) ||
                                         (Parametro.Linea != null && Parametro.Linea.Contains(busqueda)));
            }

            return query;
        }

        public async Task<IEnumerable<Parametro>> ObtenerParametrosPaginadosAsync(
            int? identificadorId, 
            string? estatus, 
            DateTime? fechaInicio, 
            DateTime? fechaFin, 
            string? busqueda, 
            int pagina, 
            int registrosPorPagina)
        {
            var query = ConstruirFiltroParametros(identificadorId, estatus, fechaInicio, fechaFin, busqueda);

            return await query.OrderByDescending(Parametro => Parametro.IdRegistro)
                              .Skip((pagina - 1) * registrosPorPagina)
                              .Take(registrosPorPagina)
                              .ToListAsync();
        }

        public async Task<int> ContarParametrosTotalAsync(
            int? identificadorId, 
            string? estatus, 
            DateTime? fechaInicio, 
            DateTime? fechaFin, 
            string? busqueda)
        {
            var query = ConstruirFiltroParametros(identificadorId, estatus, fechaInicio, fechaFin, busqueda);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<int>> ObtenerSalidasPorMaquinaAsync(int idMaquina)
        {
            return await _context.Parametros
                                 .AsNoTracking()
                                 .Where(Parametro => Parametro.IdentificadorId == idMaquina &&
                                  Parametro.Salida.HasValue)
                                 .Select(Parametro => Parametro.Salida!.Value)
                                 .Distinct()
                                 .OrderBy(salida => salida)
                                 .ToListAsync();
        }

        public async Task<Parametro?> AgregarParametroIngestaAsync(Parametro parametro)
        {
            parametro.FechaCreacion = DateTime.Now;
            var entityEntry = await _context.Parametros.AddAsync(parametro);
            return entityEntry.Entity;
        }
    }
    // Agregar y actualizar maquinas, 
}