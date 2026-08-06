using Enhartt.Domain.Data;
using Enhartt.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Enhartt.Api.Services
{
    public class RecetaService : IRecetaService
    {
        private readonly AppDbContext _context;

        public RecetaService(AppDbContext context)
        {
            _context = context;
        }

        public async Task GuardarCambiosRecetaAsync(string celda, string salida, List<HistorialReceta> cambios)
        {
            foreach (var item in cambios)
            {
                item.Celda = celda;
                item.Salida = salida;
                item.FechaModificacion = DateTime.Now;
                _context.HistorialRecetas.Add(item);
            }
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<HistorialComparativoDto>> ObtenerHistorialAsync(string? celda, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.HistorialRecetas.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(celda))
                query = query.Where(h => h.Celda == celda);

            if (fechaInicio.HasValue)
                query = query.Where(h => h.FechaModificacion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(h => h.FechaModificacion <= fechaFin.Value.AddDays(1));

            var resultado = await query.OrderByDescending(h => h.FechaModificacion).ToListAsync();

            return resultado.Select(h => new HistorialComparativoDto
            {
                IdHistorial = h.IdHistorial,
                Celda = h.Celda,
                Salida = h.Salida,
                Parametro = h.Parametro,
                ValorAnterior = $"Mín: {h.MinAnterior} | Máx: {h.MaxAnterior}",
                ValorNuevo = $"Mín: {h.MinNuevo} | Máx: {h.MaxNuevo}",
                Fecha = h.FechaModificacion.ToString("yyyy-MM-dd HH:mm:ss"),
                Usuario = h.Usuario
            });
        }
    }
}