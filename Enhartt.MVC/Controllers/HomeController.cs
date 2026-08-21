using Enhartt.Domain.Repositories;
using Enhartt.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly EnharttService enharttService;
    private readonly IRepository repository;

    public HomeController(EnharttService enharttService, IRepository repository)
    {
        this.enharttService = enharttService;
        this.repository = repository;
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerCeldas()
    {
        try
        {
            var maquinas = await enharttService.ObtenerMaquinasAsync() ?? [];
            var celdas = maquinas
                .Where(m => !string.IsNullOrEmpty(m.Celda))
                .Select(m => new { id = m.Id, celda = m.Celda, idMaquina = m.IdMaquina })
                .ToList();

            return Json(celdas);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ExportarExcel(
        int? identificadorId,
        string? estatus,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? busqueda)
    {
        try
        {
            string? estatusLimpio = NormalizarEstatus(estatus);

            // Ejecución secuencial para no colisionar el DbContext
            var registros = await repository.ObtenerParametrosPaginadosAsync(
                identificadorId, estatusLimpio, fechaInicio, fechaFin, busqueda, pagina: 1, registrosPorPagina: int.MaxValue);

            var maquinas = (await enharttService.ObtenerMaquinasAsync()) ?? [];
            var dictMaquinas = maquinas.ToDictionary(m => m.Id, m => m.Celda ?? "CEN-01");

            var builder = new System.Text.StringBuilder();
            builder.AppendLine("Fecha/Hora,Celda,Salida,Turno,N° Soldadura,Corriente (A),Energia (J),Tiempo (ms),Penetracion (mm),Estatus,Detalle Desviacion");

            foreach (var p in registros)
            {
                string celda = p.IdentificadorId.HasValue && dictMaquinas.TryGetValue(p.IdentificadorId.Value, out var c) ? c : "CEN-01";
                string fecha = p.Fecha.HasValue ? p.Fecha.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-";
                string estatusCalidad = string.IsNullOrEmpty(p.EstatusCalidad) ? "OK" : p.EstatusCalidad;
                string detalles = string.IsNullOrEmpty(p.DetallesFallas) ? "-" : p.DetallesFallas.Replace("\"", "\"\"");

                builder.AppendLine($"\"{fecha}\",\"{celda}\",\"Salida {p.Salida ?? 1}\",\"Turno 1\",\"#{p.NumSol ?? p.IdRegistro}\",\"{p.Corriente ?? 0}\",\"{p.Energia ?? 0}\",\"{p.Tiempo ?? 0}\",\"{p.Penetracion ?? 0}\",\"{estatusCalidad}\",\"{detalles}\"");
            }

            byte[] buffer = System.Text.Encoding.UTF8.GetPreamble().Concat(System.Text.Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
            return File(buffer, "text/csv", $"Reporte_Completo_Soldaduras_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
        }
        catch (Exception ex)
        {
            return BadRequest($"Error al generar el reporte: {ex.Message}");
        }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerTodosParaImpresion(
        int? identificadorId,
        string? estatus,
        DateTime? fechaInicio,
        DateTime? fechaFin,
        string? busqueda)
    {
        try
        {
            string? estatusLimpio = NormalizarEstatus(estatus);

            // Ejecución secuencial para no colisionar el DbContext
            var registros = await repository.ObtenerParametrosPaginadosAsync(
                identificadorId, estatusLimpio, fechaInicio, fechaFin, busqueda, pagina: 1, registrosPorPagina: int.MaxValue);

            var maquinas = (await enharttService.ObtenerMaquinasAsync()) ?? [];
            var dictMaquinas = maquinas.ToDictionary(m => m.Id, m => m.Celda ?? "CEN-01");

            var dataFormateada = registros.Select(p => new
            {
                Celda = p.IdentificadorId.HasValue && dictMaquinas.TryGetValue(p.IdentificadorId.Value, out var c) ? c : "CEN-01",
                Salida = p.Salida ?? 1,
                Turno = 1,
                NumSol = p.NumSol ?? p.IdRegistro,
                Corriente = p.Corriente ?? 0,
                Energia = p.Energia ?? 0,
                Tiempo = p.Tiempo ?? 0,
                Penetracion = p.Penetracion ?? 0,
                EstatusCalidad = string.IsNullOrEmpty(p.EstatusCalidad) ? "OK" : p.EstatusCalidad,
                DetallesFallas = string.IsNullOrEmpty(p.DetallesFallas) ? "-" : p.DetallesFallas,
                FechaFormatted = p.Fecha.HasValue ? p.Fecha.Value.ToString("yyyy-MM-dd HH:mm:ss") : "-"
            }).ToList();

            return Json(new { success = true, data = dataFormateada });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> RevalidarCalidad([FromBody] RevalidarCalidadDto dto)
    {
        try
        {
            var (total, cambiaronOk, cambiaronNok) = await repository.RevalidarParametrosAsync(
                dto.IdentificadorId,
                dto.FechaInicio,
                dto.FechaFin);

            string mensaje = $"Revalidación completada: {total} registros procesados ({cambiaronOk} pasaron a OK | {cambiaronNok} cambiaron a NOK).";

            return Json(new
            {
                success = true,
                message = mensaje
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error en revalidación: {ex.Message}" });
        }
    }

    public class RevalidarCalidadDto
    {
        public int? IdentificadorId { get; set; }
        public DateTime? FechaInicio { get; set; }
        public DateTime? FechaFin { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerParametros(
       int? identificadorId,
       string? estatus,
       DateTime? fechaInicio,
       DateTime? fechaFin,
       string? busqueda,
       int pagina = 1,
       int registrosPorPagina = 5)
    {
        try
        {
            string? estatusLimpio = NormalizarEstatus(estatus);

            var registros = await repository.ObtenerParametrosPaginadosAsync(
                identificadorId, estatusLimpio, fechaInicio, fechaFin, busqueda, pagina, registrosPorPagina);

            var (totalDisparos, okCount, nokCount) = await repository.ObtenerResumenKpisAsync(
                identificadorId, fechaInicio, fechaFin, busqueda);

            var maquinas = await enharttService.ObtenerMaquinasAsync() ?? [];
            var dictMaquinas = maquinas.ToDictionary(m => m.Id, m => m.Celda ?? "CEN-01");

            int totalFiltradoTabla = string.IsNullOrEmpty(estatusLimpio)
                ? totalDisparos
                : (estatusLimpio == "OK" ? okCount : nokCount);

            double efectividad = totalDisparos > 0
                ? Math.Round((double)okCount / totalDisparos * 100, 1)
                : 0;

            var dataFormateada = registros.Select(p => new
            {
                p.IdRegistro,
                p.IdentificadorId,
                Celda = p.IdentificadorId.HasValue && dictMaquinas.TryGetValue(p.IdentificadorId.Value, out var c) ? c : "CEN-01",
                Salida = p.Salida ?? 1,
                Turno = 1,
                p.NumSol,
                p.Corriente,
                p.Energia,
                p.Tiempo,
                p.Penetracion,
                p.VolArc,
                p.VolPri,
                p.Elevacion,
                p.LonPer,
                Err = 0,
                Alarma = 0,
                Modo = 1,
                EstatusCalidad = string.IsNullOrEmpty(p.EstatusCalidad) ? "OK" : p.EstatusCalidad,
                DetallesFallas = string.IsNullOrEmpty(p.DetallesFallas) ? "-" : p.DetallesFallas,
                SerialTrazabilidad = $"TCK-M{p.IdentificadorId ?? 0}-S{p.Salida ?? 1}-#{p.NumSol ?? p.IdRegistro}",
                FechaFormatted = p.Fecha.HasValue ? p.Fecha.Value.ToString("yyyy-MM-dd HH:mm:ss") : DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });

            return Json(new
            {
                success = true,
                data = dataFormateada,
                total = totalFiltradoTabla,
                paginaActual = pagina,
                totalPaginas = (int)Math.Ceiling((double)totalFiltradoTabla / Math.Max(1, registrosPorPagina)),
                kpis = new
                {
                    totalDisparos = totalDisparos,
                    calidadOk = okCount,
                    desviacionesNok = nokCount,
                    efectividadFtt = efectividad
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    private static string? NormalizarEstatus(string? estatus)
    {
        if (string.IsNullOrWhiteSpace(estatus)) return null;
        if (estatus.Contains("OK") && !estatus.Contains("NOK")) return "OK";
        if (estatus.Contains("NOK")) return "NOK";
        return estatus.Trim();
    }
}