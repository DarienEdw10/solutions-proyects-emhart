using Enhartt.Domain.Models;
using Enhartt.Domain.Repositories;

namespace Enhartt.Api.Services
{
    public class ParametroService : IParametroService
    {
        private readonly IRepository<Parametro> _parametroRepository;

        public ParametroService(IRepository<Parametro> parametroRepository)
        {
            _parametroRepository = parametroRepository;
        }

        public async Task<ResultadoPaginadoDto<object>> ObtenerParametrosPaginadosAsync(int pagina = 1, int registrosPorPagina = 5)
        {
            var datos = await _parametroRepository.GetAllAsync();
            var totalRegistros = datos.Count();

            // Paginación a nivel de servidor / consulta
            var datosPaginados = datos.OrderByDescending(p => p.IdRegistro)
                                      .Skip((pagina - 1) * registrosPorPagina)
                                      .Take(registrosPorPagina);

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
                RegistrosPorPagina = registrosPorPagina
            };
        }

        public async Task<ResumenKpiDto> ObtenerResumenKpisAsync()
        {
            var datos = await _parametroRepository.GetAllAsync();
            var lista = datos.ToList();

            int total = lista.Count;
            if (total == 0) return new ResumenKpiDto();

            int ok = lista.Count(p => (p.EstatusCalidad ?? "OK").Trim().ToUpper() == "OK");
            int nok = total - ok;
            double ftt = Math.Round(((double)ok / total) * 100, 1);

            // Obtener el registro más reciente por cada Celda
            var ultimoCelda1 = lista.Where(p => p.IdentificadorId != 3)
                                    .OrderByDescending(p => p.Fecha)
                                    .FirstOrDefault();

            var ultimoCelda3 = lista.Where(p => p.IdentificadorId == 3)
                                    .OrderByDescending(p => p.Fecha)
                                    .FirstOrDefault();

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