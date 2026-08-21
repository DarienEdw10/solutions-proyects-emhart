using Enhartt.Domain.Repositories;
using Enhartt.MVC.Models.ViewModels;
using Enhartt.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Logger = Magna.Cosma.Autotek.Log.Logger;

namespace Enhartt.MVC.Controllers;

[Authorize]
public class RecetasController : Controller
{
    private readonly IRepository _repository;
    private readonly RepositorioEmpleados _repositorioEmpleados;
    private readonly Logger _logger;

    public RecetasController(
        IRepository repository,
        RepositorioEmpleados repositorioEmpleados,
        Logger logger)
    {
        _repository = repository;
        _repositorioEmpleados = repositorioEmpleados;
        _logger = logger;
    }

    private async Task<bool> TienePermisoRecetasAsync()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;

        string cwid = User.Identity?.Name ?? "";
        if (cwid.Contains('\\'))
        {
            cwid = cwid.Split('\\')[1];
        }

        if (string.IsNullOrEmpty(cwid))
        {
            cwid = Environment.UserName;
        }

        // 1. Validación en DLL corporativa (Magna Autotek)
        try
        {
            var empleadoCorporativo = _repositorioEmpleados.ObtenerEmpleadoPorCWID(cwid);
            
            // Si el empleado está dado de baja formalmente en la planta
            if (empleadoCorporativo != null && !empleadoCorporativo.Activo)
            {
                _logger.Registrar(
                    nivel: Logger.NivelesLog.Basico,
                    tipo: Logger.TiposLog.Errores,
                    origen: "RecetasController.Permisos",
                    texto: $"Acceso denegado: El empleado [{cwid}] se encuentra inactivo en la base corporativa.");
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.Registrar(
                nivel: Logger.NivelesLog.Basico,
                tipo: Logger.TiposLog.Errores,
                origen: "RecetasController.Permisos",
                texto: $"Error al consultar DLL corporativa para el usuario [{cwid}]: {ex.Message}");
        }

        // 2. Validación de nivel en base de datos local (Nivel >= 20 para Calidad/Supervisores/Sistemas)
        try
        {
            int nivelUsuario = await _repository.ObtenerNivelUsuarioPorCWIDAsync(cwid);
            return nivelUsuario >= 20;
        }
        catch (Exception ex)
        {
            _logger.Registrar(
                nivel: Logger.NivelesLog.Basico,
                tipo: Logger.TiposLog.Errores,
                origen: "RecetasController.Permisos",
                texto: $"Error al validar permisos de usuario [{cwid}] en BD: {ex.Message}");
            return false;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!await TienePermisoRecetasAsync()) return Forbid();
        return await CargarVistaRecetasAsync("recetas");
    }

    [HttpGet]
    public async Task<IActionResult> Recetas()
    {
        if (!await TienePermisoRecetasAsync()) return Forbid();
        return await CargarVistaRecetasAsync("recetas");
    }

    [HttpGet]
    public async Task<IActionResult> AltasRecetas()
    {
        if (!await TienePermisoRecetasAsync()) return Forbid();
        return await CargarVistaRecetasAsync("AltasRecetas");
    }

    private async Task<IActionResult> CargarVistaRecetasAsync(string vistaNombre)
    {
        var maquinas = (await _repository.ObtenerMaquinasAsync(soloActivos: true))?.ToList() ?? new();
        var todasLasRecetas = (await _repository.ObtenerTodasLasRecetasActivasAsync())?.ToList() ?? new();

        var recetasPorMaquina = todasLasRecetas
            .GroupBy(r => r.IdMaquina)
            .ToDictionary(grp => grp.Key, grp => grp.ToList());

        var celdas = maquinas
            .Where(m => !string.IsNullOrEmpty(m.Celda))
            .GroupBy(m => m.Celda!)
            .Select(grp => new CeldaViewModel
            {
                Celda = grp.Key,
                Maquinas = grp.Select(maquina => new MaquinaViewModel
                {
                    Id = maquina.Id,
                    IdMaquina = maquina.IdMaquina ?? "",
                    Recetas = (recetasPorMaquina.TryGetValue(maquina.Id, out var recs) ? recs : new())
                        .Select(r => new RecetaViewModel
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
                }).ToList()
            }).ToList();

        return View(vistaNombre, new RecetasViewModel() { Celdas = celdas });
    }

    [HttpPost]
    public async Task<IActionResult> Guardar([FromBody] GuardarRecetaDto dto)
    {
        if (!await TienePermisoRecetasAsync()) return Forbid();

        if (dto == null || dto.Parametros.Count == 0)
        {
            return BadRequest(new { success = false, message = "No se recibieron parámetros válidos para guardar." });
        }

        string usuario = User?.Identity?.Name ?? "Usuario_Web";

        try
        {
            int.TryParse(dto.Salida, out int numSalida);

            string motivo = string.IsNullOrWhiteSpace(dto.Comentario)
                ? "Ajuste operativo de parámetros"
                : dto.Comentario.Trim();

            foreach (var p in dto.Parametros)
            {
                var nuevaReceta = new Enhartt.Domain.Models.Receta
                {
                    IdMaquina = dto.IdMaquina,
                    Salida = numSalida,
                    Parametro = p.Parametro?.Trim() ?? "",
                    MinVal = p.MinVal,
                    MaxVal = p.MaxVal,
                    Estado = true,
                    FechaCreacion = DateTime.Now,
                    ModificadoPor = usuario,
                    Comentario = motivo
                };

                await _repository.ActualizarRecetaConHistorialAsync(nuevaReceta, usuario);
            }

            _logger.Registrar(
                nivel: Logger.NivelesLog.Detallado,
                tipo: Logger.TiposLog.Informativo,
                origen: "RecetasController.Guardar",
                texto: $"El usuario [{usuario}] modificó tolerancias en Máquina ID [{dto.IdMaquina}], Salida [{dto.Salida}]. Motivo: [{motivo}]");

            return Json(new { success = true, message = "Los límites y el motivo de cambio fueron registrados exitosamente." });
        }
        catch (Exception ex)
        {
            _logger.Registrar(
                nivel: Logger.NivelesLog.Basico,
                tipo: Logger.TiposLog.Errores,
                origen: "RecetasController.Guardar",
                texto: $"Error al guardar receta para usuario [{usuario}]: {ex.Message}");

            return StatusCode(500, new { success = false, message = $"Error en el servidor: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerAuditoria(int? idMaquina, string? salida, DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (!await TienePermisoRecetasAsync()) return Forbid();

        try
        {
            var maquinas = (await _repository.ObtenerMaquinasAsync(soloActivos: false))?.ToList() ?? new();
            var historial = await _repository.ObtenerAuditoriaRecetasAsync(idMaquina, salida, fechaInicio, fechaFin);

            var resultadoDto = historial.Select(h => new
            {
                Fecha = (h.FechaModificacion ?? h.FechaCreacion).ToString("yyyy-MM-dd HH:mm:ss"),
                Celda = maquinas.FirstOrDefault(m => m.Id == h.IdMaquina)?.Celda ?? "CEN-01",
                Salida = h.Salida,
                Parametro = h.Parametro ?? "Límite Control",
                MinVal = h.MinVal,
                MaxVal = h.MaxVal,
                Estado = h.Estado ? "ACTIVO" : "INACTIVO",
                Usuario = string.IsNullOrEmpty(h.ModificadoPor) ? "SISTEMA_INICIAL" : h.ModificadoPor,
                Comentario = string.IsNullOrWhiteSpace(h.Comentario) ? "-" : h.Comentario
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
        if (!await TienePermisoRecetasAsync()) return Forbid();

        if (dto == null || dto.IdMaquina <= 0 || dto.NumeroSalida <= 0)
        {
            return BadRequest(new { success = false, message = "Datos de máquina o salida no válidos." });
        }

        string usuario = User?.Identity?.Name ?? "Usuario_Web";

        try
        {
            var recetasExistentes = await _repository.ObtenerRecetasPorMaquinaAsync(dto.IdMaquina) ?? [];

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
                    ModificadoPor = usuario,
                    Comentario = "Alta de nueva salida"
                };

                await _repository.AgregarRecetaAsync(nuevaReceta, usuario);
            }

            _logger.Registrar(
                nivel: Logger.NivelesLog.Detallado,
                tipo: Logger.TiposLog.Informativo,
                origen: "RecetasController.AgregarSalida",
                texto: $"El usuario [{usuario}] dio de alta la Salida [{dto.NumeroSalida}] en Máquina ID [{dto.IdMaquina}].");

            return Json(new { success = true, message = $"Salida {dto.NumeroSalida} agregada e inicializada correctamente." });
        }
        catch (Exception ex)
        {
            _logger.Registrar(
                nivel: Logger.NivelesLog.Basico,
                tipo: Logger.TiposLog.Errores,
                origen: "RecetasController.AgregarSalida",
                texto: $"Error al crear salida para usuario [{usuario}]: {ex.Message}");

            return StatusCode(500, new { success = false, message = $"Error al crear salida: {ex.Message}" });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AgregarCelda([FromBody] AgregarCeldaDto dto)
    {
        if (!await TienePermisoRecetasAsync()) return Forbid();

        if (dto == null || string.IsNullOrWhiteSpace(dto.NombreCelda) || string.IsNullOrWhiteSpace(dto.IdMaquina))
        {
            return BadRequest(new { success = false, message = "El nombre de la celda y el ID de máquina son obligatorios." });
        }

        string usuario = User?.Identity?.Name ?? "Usuario_Web";

        try
        {
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

            var maquinaCreada = await _repository.AgregarMaquinaAsync(nuevaMaquina);

            if (maquinaCreada == null)
            {
                return StatusCode(500, new { success = false, message = "No se pudo registrar la máquina en la base de datos." });
            }

            string[] parametrosBase = new string[] { "VolArc", "VolPri", "Corriente", "Tiempo", "Penetracion", "Energia" };

            foreach (var param in parametrosBase)
            {
                var recetaInicial = new Enhartt.Domain.Models.Receta
                {
                    IdMaquina = maquinaCreada.Id,
                    Salida = salidaInicial,
                    Parametro = param,
                    MinVal = 0,
                    MaxVal = 0,
                    Estado = true,
                    FechaCreacion = DateTime.Now,
                    ModificadoPor = usuario,
                    Comentario = "Alta de celda inicial"
                };

                await _repository.AgregarRecetaAsync(recetaInicial, usuario);
            }

            _logger.Registrar(
                nivel: Logger.NivelesLog.Detallado,
                tipo: Logger.TiposLog.Informativo,
                origen: "RecetasController.AgregarCelda",
                texto: $"El usuario [{usuario}] dio de alta la Celda [{nuevaMaquina.Celda}] (Máquina: {nuevaMaquina.IdMaquina}).");

            return Json(new { success = true, message = $"Celda '{nuevaMaquina.Celda}' creada exitosamente inicializada en la Salida {salidaInicial}." });
        }
        catch (Exception ex)
        {
            _logger.Registrar(
                nivel: Logger.NivelesLog.Basico,
                tipo: Logger.TiposLog.Errores,
                origen: "RecetasController.AgregarCelda",
                texto: $"Error al crear celda para usuario [{usuario}]: {ex.Message}");

            return StatusCode(500, new { success = false, message = $"Error al crear celda: {ex.Message}" });
        }
    }
}

public class AgregarCeldaDto
{
    public string Planta { get; set; } = string.Empty;
    public string Linea { get; set; } = string.Empty;
    public string NombreCelda { get; set; } = string.Empty;
    public string IdMaquina { get; set; } = string.Empty;
    public int SalidaInicial { get; set; } = 1;
}

public class GuardarRecetaDto
{
    public int IdMaquina { get; set; }
    public string Salida { get; set; } = string.Empty;
    public string? Comentario { get; set; }
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