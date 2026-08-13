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
        return await CargarVistaRecetasAsync("recetas");
    }

    [HttpGet]
    public async Task<IActionResult> Recetas()
    {
        return await CargarVistaRecetasAsync("recetas");
    }

    // Nueva Acción GET: Renderiza la pantalla "Altas Recetas"
    [HttpGet]
    public async Task<IActionResult> AltasRecetas()
    {
        return await CargarVistaRecetasAsync("AltasRecetas");
    }

    private async Task<IActionResult> CargarVistaRecetasAsync(string vistaNombre)
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

        return View(vistaNombre, new RecetasViewModel() { Celdas = celdasVM });
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
                Celda = maquinas.FirstOrDefault(m => m.Id == h.IdMaquina)?.Celda ?? "CEN-01",
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
            var recetasExistentes = await repository.ObtenerRecetasPorMaquinaAsync(dto.IdMaquina) ?? [];

            string[] parametrosBase = new string[] { "VolArc", "VolPri", "Corriente", "Tiempo", "Penetracion", "Energia" };

            foreach (var param in parametrosBase)
            {
                var recetaReferencia = recetasExistentes.FirstOrDefault(r =>
                    r.Parametro != null && r.Parametro.Equals(param, StringComparison.OrdinalIgnoreCase));

                double minVal = recetaReferencia?.MinVal ?? 0;
                double maxVal = recetaReferencia?.MaxVal ?? 0;

                var nuevaReceta = new Enhartt.Domain.Models.Receta
                {
                    IdMaquina = dto.IdMaquina,
                    Salida = dto.NumeroSalida,
                    Parametro = param,
                    MinVal = minVal,
                    MaxVal = maxVal,
                    Estado = true,
                    FechaCreacion = DateTime.Now,
                    ModificadoPor = usuario
                };

                await repository.AgregarRecetaAsync(nuevaReceta, usuario);
            }

            return Json(new { success = true, message = $"Salida {dto.NumeroSalida} agregada e inicializada correctamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error al crear salida: {ex.Message}" });
        }
    }

[HttpPost]
public async Task<IActionResult> AgregarCelda([FromBody] AgregarCeldaDto dto)
{
    if (dto == null || string.IsNullOrWhiteSpace(dto.NombreCelda) || string.IsNullOrWhiteSpace(dto.IdMaquina))
    {
        return BadRequest(new { success = false, message = "El nombre de la celda y el ID de máquina son obligatorios." });
    }

    try
    {
        string usuario = User?.Identity?.Name ?? "Usuario_Web";
        int salidaInicial = dto.SalidaInicial > 0 ? dto.SalidaInicial : 1;

        var nuevaMaquina = new Enhartt.Domain.Models.Maquina
        {
            Planta = string.IsNullOrWhiteSpace(dto.Planta) ? "AUTOTEK CUAUTITLÁN" : dto.Planta.Trim().ToUpper(),
            Linea = string.IsNullOrWhiteSpace(dto.Linea) ? "LÍNEA 1" : dto.Linea.Trim().ToUpper(),
            Celda = dto.NombreCelda.Trim().ToUpper(),
            Estacion = "ESTACIÓN 1",
            IdMaquina = dto.IdMaquina.Trim().ToUpper(),
            FechaCreacion = DateTime.Now
        };

        var maquinaCreada = await repository.AgregarMaquinaAsync(nuevaMaquina);

        if (maquinaCreada == null)
        {
            return StatusCode(500, new { success = false, message = "No se pudo registrar la máquina en la base de datos." });
        }

        string[] parametrosBase = new string[] { "VolArc", "VolPri", "Corriente", "Tiempo", "Penetracion", "Energia" };

        // Insertar los 6 parámetros para la Salida especificada por el usuario
        foreach (var param in parametrosBase)
        {
            var recetaInicial = new Enhartt.Domain.Models.Receta
            {
                IdMaquina = maquinaCreada.Id,
                Salida = salidaInicial, // <-- Usa la salida elegida (ej. 4)
                Parametro = param,
                MinVal = 0,
                MaxVal = 0,
                Estado = true,
                FechaCreacion = DateTime.Now,
                ModificadoPor = usuario
            };

            await repository.AgregarRecetaAsync(recetaInicial, usuario);
        }

        return Json(new { success = true, message = $"Celda '{nuevaMaquina.Celda}' creada exitosamente inicializada en la Salida {salidaInicial}." });
    }
    catch (Exception ex)
    {
        return StatusCode(500, new { success = false, message = $"Error al crear celda: {ex.Message}" });
    }
}

// Actualización del DTO
public class AgregarCeldaDto
{
    public string Planta { get; set; } = string.Empty;
    public string Linea { get; set; } = string.Empty;
    public string NombreCelda { get; set; } = string.Empty;
    public string IdMaquina { get; set; } = string.Empty;
    public int SalidaInicial { get; set; } = 1; // <-- Nueva propiedad
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

public class AgregarSalidaDto
{
    public int IdMaquina { get; set; }
    public int NumeroSalida { get; set; }
}
}
