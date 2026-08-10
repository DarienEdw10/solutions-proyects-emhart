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

        public async Task<IEnumerable<Receta>> ObtenerRecetaActivaAsync(int idMaquina, int salida)
        {
            return await _context.Receta
                                 .AsNoTracking()
                                 .Where(r => r.IdMaquina == idMaquina && r.Salida == salida && r.Estado)
                                 .ToListAsync();
        }

        public async Task ActualizarRecetaConHistorialAsync(int idMaquina, int salida, List<Receta> nuevosParametros, string usuario)
        {
            var ahora = DateTime.Now;

            var recetasActuales = await _context.Receta
                                                .Where(r => r.IdMaquina == idMaquina && r.Salida == salida && r.Estado)
                                                .ToListAsync();

            foreach (var nuevo in nuevosParametros)
            {
                var coincidencia = recetasActuales.FirstOrDefault(r => r.Parametro == nuevo.Parametro);

                if (coincidencia != null && (coincidencia.MinVal != nuevo.MinVal || coincidencia.MaxVal != nuevo.MaxVal))
                {
                    coincidencia.Estado = false;
                    coincidencia.FechaModificacion = ahora;
                    _context.Receta.Update(coincidencia);

                    var nuevaReceta = new Receta
                    {
                        IdMaquina = idMaquina,
                        Salida = salida,
                        Parametro = nuevo.Parametro,
                        MinVal = nuevo.MinVal,
                        MaxVal = nuevo.MaxVal,
                        FechaCreacion = ahora,
                        ModificadoPor = usuario,
                        Estado = true
                    };
                    await _context.Receta.AddAsync(nuevaReceta);
                }
            }

            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<RecetaHistorialDto>> ObtenerHistorialCambiosAsync(int idMaquina, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.Receta.AsNoTracking().Where(r => r.IdMaquina == idMaquina);

            if (fechaInicio.HasValue)
                query = query.Where(r => r.FechaCreacion >= fechaInicio.Value);

            if (fechaFin.HasValue)
                query = query.Where(r => r.FechaCreacion <= fechaFin.Value.AddDays(1));

            var resultado = await query.OrderByDescending(r => r.FechaCreacion).ToListAsync();

            return resultado.Select(r => new RecetaHistorialDto
            {
                IdReferencia = r.IdReferencia,
                Celda = r.IdMaquina == 3 ? "CELDA-03" : "CELDA-01",
                Salida = r.Salida,
                Parametro = r.Parametro,
                MinVal = r.MinVal,
                MaxVal = r.MaxVal,
                FechaRegistro = r.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss"),
                ModificadoPor = r.ModificadoPor,
                EstatusRegistro = r.Estado ? "ACTIVO (ACTUAL)" : "INHABILITADO (ANTERIOR)"
            });
        }
    }
}