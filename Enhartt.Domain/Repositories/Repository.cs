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
            Receta? recetaBd = await _context.Receta.FirstOrDefaultAsync(r => r.IdReferencia == receta.IdReferencia);

            if (recetaBd == null) return null;

            recetaBd.MinVal = receta.MinVal;
            recetaBd.MaxVal = receta.MaxVal;
            recetaBd.Estado = receta.Estado;
            recetaBd.ModificadoPor = usuario;
            recetaBd.FechaModificacion = DateTime.Now;

            var entityEntry = _context.Receta.Update(recetaBd);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        public async Task<Receta?> AgregarRecetaAsync(Receta receta, string usuario)
        {
            receta.Estado = true;
            receta.FechaCreacion = DateTime.Now;
            receta.ModificadoPor = usuario;
            var entityEntry = await _context.Receta.AddAsync(receta);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        public async Task<IEnumerable<Receta>> ObtenerAuditoriaRecetasAsync(int? idMaquina, string? salida, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.Receta
                                 .AsNoTracking()
                                 .AsQueryable();

            if (idMaquina.HasValue && idMaquina.Value > 0)
            {
                query = query.Where(r => r.IdMaquina == idMaquina.Value);
            }

            if (!string.IsNullOrEmpty(salida) && int.TryParse(salida, out int numSalida))
            {
                query = query.Where(r => r.Salida == numSalida);
            }

            if (fechaInicio.HasValue)
            {
                var inicio = fechaInicio.Value.Date;
                query = query.Where(r => (r.FechaModificacion ?? r.FechaCreacion) >= inicio);
            }

            if (fechaFin.HasValue)
            {
                var fin = fechaFin.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => (r.FechaModificacion ?? r.FechaCreacion) <= fin);
            }

            return await query.OrderByDescending(r => r.FechaModificacion ?? r.FechaCreacion)
                              .ToListAsync();
        }

        // =============================================================
        // MAQUINAS
        // =============================================================
        public async Task<IEnumerable<Maquina>> ObtenerMaquinasAsync(bool soloActivos = true)
        {
            var query = _context.Maquinas
                                 .AsNoTracking()
                                 .AsQueryable();

            if (soloActivos) query = query.Where(maquina => maquina.Activo == true);

            return await query.ToListAsync();
        }

        public async Task<Maquina?> ObtenerMaquinaPorIdAsync(string idMaquina)
        {
            return await _context.Maquinas
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(m => m.IdMaquina == idMaquina);
        }

        public async Task<Maquina?> AgregarMaquinaAsync(Maquina maquina)
        {
            maquina.Activo = true;
            maquina.FechaCreacion = DateTime.Now;
            var entityEntry = await _context.Maquinas.AddAsync(maquina);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        public async Task<Maquina?> ActualizarMaquinaAsync(Maquina maquina)
        {
            Maquina? maquinaBd = await _context.Maquinas.FirstOrDefaultAsync(m => m.Id == maquina.Id || m.IdMaquina == maquina.IdMaquina);

            if (maquinaBd == null) return null;

            maquinaBd.Planta = maquina.Planta;
            maquinaBd.Linea = maquina.Linea;
            maquinaBd.Celda = maquina.Celda;
            maquinaBd.Estacion = maquina.Estacion;
            maquinaBd.Modelo = maquina.Modelo;
            maquinaBd.Activo = maquina.Activo;
            maquinaBd.FechaModificacion = DateTime.Now;

            var entityEntry = _context.Maquinas.Update(maquinaBd);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
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

            if (identificadorId.HasValue && identificadorId.Value > 0)
                query = query.Where(parametro => parametro.IdentificadorId == identificadorId.Value);

            if (!string.IsNullOrEmpty(estatus))
                query = query.Where(parametro => parametro.EstatusCalidad == estatus);

            if (fechaInicio.HasValue)
                query = query.Where(parametro => parametro.Fecha >= fechaInicio.Value.Date);

            if (fechaFin.HasValue)
                query = query.Where(parametro => parametro.Fecha <= fechaFin.Value.Date.AddDays(1).AddTicks(-1));

            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(parametro => (parametro.NumSol.HasValue && EF.Functions.Like(parametro.NumSol.Value.ToString(), $"%{busqueda}%")) ||
                                                 (parametro.DetallesFallas != null && parametro.DetallesFallas.Contains(busqueda)) ||
                                                 (parametro.Linea != null && parametro.Linea.Contains(busqueda)));
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

            return await query.OrderByDescending(parametro => parametro.IdRegistro)
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
                                 .Where(parametro => parametro.IdentificadorId == idMaquina && parametro.Salida.HasValue)
                                 .Select(parametro => parametro.Salida!.Value)
                                 .Distinct()
                                 .OrderBy(salida => salida)
                                 .ToListAsync();
        }

        public async Task<Parametro?> AgregarParametroIngestaAsync(Parametro parametro)
        {
            parametro.FechaCreacion = DateTime.Now;
            var entityEntry = await _context.Parametros.AddAsync(parametro);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }
    }
}