using Enhartt.Domain.Data;
using Enhartt.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace Enhartt.Api.Services
{
    public class ParametroService : IParametroService
    {
        private readonly AppDbContext _context;

        public ParametroService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ResultadoPaginadoDto<object>> ObtenerParametrosPaginadosAsync(
            int pagina = 1,
            int registrosPorPagina = 5,
            string? celda = null,
            string? estatus = null,
            string? turno = null,
            string? busqueda = null)
        {
            // Iniciar consulta IQueryable sobre SQL Server
            var query = _context.Parametros.AsNoTracking().AsQueryable();

            // 1. Filtro por Celda (1 = CELDA-01, 3 = CELDA-03)
            if (!string.IsNullOrEmpty(celda) && celda != "-- Todas --")
            {
                int idMaquina = celda == "CELDA-03" ? 3 : 1;
                query = query.Where(p => p.IdentificadorId == idMaquina);
            }

            // 2. Filtro por Estatus de Calidad
            if (!string.IsNullOrEmpty(estatus) && estatus != "-- Todos --")
            {
                if (estatus == "Solo OK" || estatus == "OK")
                    query = query.Where(p => p.EstatusCalidad != null && p.EstatusCalidad.Trim().ToUpper() == "OK");
                else if (estatus == "Solo NOK" || estatus == "NOK")
                    query = query.Where(p => p.EstatusCalidad != null && p.EstatusCalidad.Trim().ToUpper() != "OK");
            }

            // 3. Filtro por Búsqueda Rápida (Coincidencia parcial en N° Soldadura o Fallas)
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(p => (p.NumSol != null && p.NumSol.ToString().Contains(busqueda)) ||
                                         (p.DetallesFallas != null && p.DetallesFallas.Contains(busqueda)) ||
                                         (p.Linea != null && p.Linea.Contains(busqueda)));
            }

            // Conteo exacto en SQL Server según los filtros aplicados
            var totalRegistros = await query.CountAsync();

            // Paginación eficiente a nivel SQL (OFFSET y FETCH)
            var datosPaginados = await query.OrderByDescending(p => p.IdRegistro)
                                           .Skip((pagina - 1) * registrosPorPagina)
                                           .Take(registrosPorPagina)
                                           .ToListAsync();

            var elementosMapped = datosPaginados.Select(p => new
            {
                fecha = p.Fecha.HasValue ? p.Fecha.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                celda = p.IdentificadorId == 3 ? "CELDA-03" : "CELDA-01",
                salida = $"Out {p.Salida ?? 1}",
                programa = p.Programa ?? 1,
                numSol = p.NumSol ?? 0,
                corriente = p.Corriente ?? 0,
                energia = p.Energia ?? 0,
                tiempo = p.Tiempo ?? 0,
                penetracion = p.Penetracion ?? 0,
                volArc = p.VolArc ?? 0,
                volPri = p.VolPri ?? 0,
                elevacion = p.Elevacion ?? 0,
                caida = p.Caida ?? 0,
                lonPer = p.LonPer ?? 0,
                estatus = (p.EstatusCalidad ?? "OK").Trim().ToUpper() == "OK" ? "OK" : "NOK",
                detalles = p.DetallesFallas ?? "",
                turno = "T1"

            });

            return new ResultadoPaginadoDto<object>
            {
                Elementos = elementosMapped,
                TotalRegistros = totalRegistros,
                PaginaActual = pagina,
                TamanoPagina = registrosPorPagina
            };
        }

        public async Task<ResumenKpiDto> ObtenerResumenKpisAsync()
        {
            var query = _context.Parametros.AsNoTracking();

            int total = await query.CountAsync();
            if (total == 0) return new ResumenKpiDto();

            int ok = await query.CountAsync(p => p.EstatusCalidad != null && p.EstatusCalidad.Trim().ToUpper() == "OK");
            int nok = total - ok;
            double ftt = Math.Round(((double)ok / total) * 100, 1);

            var ultimoCelda1 = await query.Where(p => p.IdentificadorId != 3)
                                          .OrderByDescending(p => p.Fecha)
                                          .FirstOrDefaultAsync();

            var ultimoCelda3 = await query.Where(p => p.IdentificadorId == 3)
                                          .OrderByDescending(p => p.Fecha)
                                          .FirstOrDefaultAsync();

            return new ResumenKpiDto
            {
                TotalDisparos = total,
                CalidadOk = ok,
                DesviacionesNok = nok,
                EfectividadFtt = ftt,
                UltimoDisparoCelda1 = ObtenerTiempoRelativo(ultimoCelda1?.Fecha),
                UltimoDisparoCelda3 = ObtenerTiempoRelativo(ultimoCelda3?.Fecha),
                EstatusCelda1 = (ultimoCelda1?.EstatusCalidad ?? "OK").Trim().ToUpper() == "OK" ? "OPERANDO" : "ATENCIÓN (1 NOK)",
                EstatusCelda3 = (ultimoCelda3?.EstatusCalidad ?? "OK").Trim().ToUpper() == "OK" ? "OPERANDO" : "ATENCIÓN (1 NOK)"
            };
        }

        private string ObtenerTiempoRelativo(DateTime? fecha)
        {
            if (!fecha.HasValue) return "Sin datos";
            var diff = DateTime.Now - fecha.Value;
            if (diff.TotalSeconds < 60) return $"hace {Math.Max(1, (int)diff.TotalSeconds)}s";
            if (diff.TotalMinutes < 60) return $"hace {(int)diff.TotalMinutes}m";
            return $"hace {(int)diff.TotalHours}h";
        }
    }
}