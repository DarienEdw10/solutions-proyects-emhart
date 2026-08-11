using Enhartt.MVC.Models.ViewModels;
using Enhartt.MVC.Services;
using Enhartt.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers;

public class RecetasController : Controller
{
    private readonly EnharttService enharttService;
    private readonly IRepository repository;

    public RecetasController(EnharttService enharttService, IRepository repository)
    {
        this.enharttService = enharttService;
        this.repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return await CargarVistaRecetasAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Recetas()
    {
        return await CargarVistaRecetasAsync();
    }

    private async Task<IActionResult> CargarVistaRecetasAsync()
    {
        var maquinas = await enharttService.ObtenerMaquinasAsync() ?? [];

        var celdas = maquinas
            .Where(m => !string.IsNullOrEmpty(m.Celda))
            .GroupBy(m => m.Celda!)
            .ToDictionary(
                grp => grp.Key,
                grp => grp.ToList()
            );

        List<CeldaViewModel> celdasVM = [];

        foreach (var celdaKVP in celdas)
        {
            List<MaquinaViewModel> maquinasVM = [];

            foreach (var maquina in celdaKVP.Value)
            {
                // Obtenemos los registros COMPLETOS de la tabla de recetas
                var recetasDb = await repository.ObtenerRecetasPorMaquinaAsync(maquina.Id) ?? [];

                maquinasVM.Add(new MaquinaViewModel
                {
                    Id = maquina.Id,
                    IdMaquina = maquina.IdMaquina ?? "",
                    Recetas = recetasDb.Select(r => new RecetaViewModel
                    {
                        Id = r.IdReferencia,
                        Salida = r.Salida.ToString(),
                        Parametro = r.Parametro ?? "Límite Control",
                        MinVal = r.MinVal,
                        MaxVal = r.MaxVal,
                        FechaModificacion = r.FechaModificacion ?? r.FechaCreacion,
                        ModificadoPor = r.ModificadoPor ?? "SISTEMA_INICIAL",
                        Estado = r.Estado
                    }).ToList()
                });
            }

            celdasVM.Add(new CeldaViewModel
            {
                Celda = celdaKVP.Key,
                Maquinas = maquinasVM
            });
        }

        return View("recetas", new RecetasViewModel()
        {
            Celdas = celdasVM
        });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] GuardarRecetaDto dto)
    {
        if (dto == null || dto.Parametros.Count == 0)
        {
            return BadRequest(new { success = false, message = "No se recibieron parámetros válidos para guardar." });
        }

        try
        {
            string usuario = User?.Identity?.Name ?? "Usuario_Web";

            // Convertir el string de salida a int si tu entidad Receta maneja Salida como int
            int.TryParse(dto.Salida, out int numSalida);

            foreach (var p in dto.Parametros)
            {
                var recetaAActualizar = new Enhartt.Domain.Models.Receta
                {
                    IdMaquina = dto.IdMaquina,
                    Salida = numSalida,
                    Parametro = p.Parametro,
                    MinVal = p.MinVal,
                    MaxVal = p.MaxVal
                };

                // Llamada al método real de tu IRepository
                await repository.ActualizarRecetaAsync(recetaAActualizar, usuario);
            }

            return Json(new { success = true, message = "Los límites de calidad fueron guardados correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error en el servidor: {ex.Message}" });
        }
    }
    [HttpGet]
    public async Task<IActionResult> ObtenerAuditoria(int? idMaquina, string? salida, DateTime? fechaInicio, DateTime? fechaFin)
    {
        try
        {
            var maquinas = await enharttService.ObtenerMaquinasAsync() ?? [];
            var historial = await repository.ObtenerAuditoriaRecetasAsync(idMaquina, salida, fechaInicio, fechaFin);

            var resultadoDto = historial.Select(h => new
            {
                Fecha = (h.FechaModificacion ?? h.FechaCreacion).ToString("yyyy-MM-dd HH:mm:ss"),
                Celda = maquinas.FirstOrDefault(m => m.Id == h.IdMaquina)?.Celda ?? "CEN-01", // <-- Agregado
                Salida = h.Salida,
                Parametro = h.Parametro ?? "Límite Control",
                MinVal = h.MinVal,
                MaxVal = h.MaxVal,
                Estado = h.Estado ? "ACTIVO" : "INACTIVO",
                Usuario = string.IsNullOrEmpty(h.ModificadoPor) ? "SISTEMA_INICIAL" : h.ModificadoPor
            });

            return Json(new { success = true, data = resultadoDto });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error al obtener auditoría: {ex.Message}" });
        }
    }
    public class AgregarSalidaDto
    {
        public int IdMaquina { get; set; }
        public int NumeroSalida { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> AgregarSalida([FromBody] AgregarSalidaDto dto)
    {
        if (dto == null || dto.IdMaquina <= 0 || dto.NumeroSalida <= 0)
        {
            return BadRequest(new { success = false, message = "Datos de máquina o salida no válidos." });
        }

        try
        {
            string usuario = User?.Identity?.Name ?? "Usuario_Web";

            // Definición de parámetros físicos estándar para la nueva salida
            string[] parametrosBase = new string[] { "VolArc", "VolPri", "Corriente", "Tiempo", "Penetracion", "Energia" };

            foreach (var param in parametrosBase)
            {
                var nuevaReceta = new Enhartt.Domain.Models.Receta
                {
                    IdMaquina = dto.IdMaquina,
                    Salida = dto.NumeroSalida,
                    Parametro = param,
                    MinVal = 0,
                    MaxVal = 0,
                    Estado = true,
                    FechaCreacion = DateTime.Now,
                    ModificadoPor = usuario
                };

                await repository.AgregarRecetaAsync(nuevaReceta, usuario);
            }

            return Json(new { success = true, message = $"Salida {dto.NumeroSalida} agregada correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error al crear salida: {ex.Message}" });
        }
    }

    public class GuardarRecetaDto
    {
        public int IdMaquina { get; set; }
        public string Salida { get; set; } = string.Empty;
        public List<ParametroLimiteDto> Parametros { get; set; } = [];
    }

    public class ParametroLimiteDto
    {
        public string Parametro { get; set; } = string.Empty;
        public double MinVal { get; set; }
        public double MaxVal { get; set; }
    }
}