using Enhartt.Domain.Repositories;
using Enhartt.MVC.Models.ViewModels;
using Enhartt.MVC.Services;
using Magna.Cosma.Autotek.VIPTRA.Foreign;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers;

[Authorize]
public class RecetasController : Controller
{
    private readonly EnharttService _enharttService;
    private readonly IRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly RepositorioEmpleados _repositorioEmpleados;

    public RecetasController(
        EnharttService enharttService, 
        IRepository repository, 
        IConfiguration configuration,
        RepositorioEmpleados repositorioEmpleados)
    {
        _enharttService = enharttService;
        _repository = repository;
        _configuration = configuration;
        _repositorioEmpleados = repositorioEmpleados;
    }

    private bool TienePermisoRecetas()
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

        // 1. Consulta corporativa en BD de Magna Autotek mediante RepositorioEmpleados
        try
        {
            var empleado = _repositorioEmpleados.ObtenerEmpleadoPorCWID(cwid);
            if (empleado != null && empleado.Activo)
            {
                return true;
            }
        }
        catch
        {
            // Respaldo en caso de desconexión momentánea de la BD de nómina/empleados
        }

        // 2. Validación de respaldo por listas en appsettings.json
        var sistemasUsers = _configuration.GetSection("PermisosSettings:Sistemas:Usuarios").Get<List<string>>() ?? new();
        var calidadUsers = _configuration.GetSection("PermisosSettings:CalidadSupervisores:Usuarios").Get<List<string>>() ?? new();

        if (sistemasUsers.Concat(calidadUsers).Any(u => u.Equals(cwid, StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        // 3. Validación por rol de Windows protegida
        try
        {
            var sistemasRoles = _configuration.GetSection("PermisosSettings:Sistemas:Roles").Get<List<string>>() ?? new();
            var calidadRoles = _configuration.GetSection("PermisosSettings:CalidadSupervisores:Roles").Get<List<string>>() ?? new();
            return sistemasRoles.Concat(calidadRoles).Any(r => User.IsInRole(r));
        }
        catch
        {
            return false;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!TienePermisoRecetas()) return Forbid();
        return await CargarVistaRecetasAsync("recetas");
    }

    [HttpGet]
    public async Task<IActionResult> Recetas()
    {
        if (!TienePermisoRecetas()) return Forbid();
        return await CargarVistaRecetasAsync("recetas");
    }

    [HttpGet]
    public async Task<IActionResult> AltasRecetas()
    {
        if (!TienePermisoRecetas()) return Forbid();
        return await CargarVistaRecetasAsync("AltasRecetas");
    }

    private async Task<IActionResult> CargarVistaRecetasAsync(string vistaNombre)
    {
        var maquinas = await _enharttService.ObtenerMaquinasAsync() ?? [];
        var todasLasRecetas = await _repository.ObtenerTodasLasRecetasActivasAsync() ?? [];

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
                    Recetas = (recetasPorMaquina.TryGetValue(maquina.Id, out var recs) ? recs : [])
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
        if (!TienePermisoRecetas()) return Forbid();

        if (dto == null || dto.Parametros.Count == 0)
        {
            return BadRequest(new { success = false, message = "No se recibieron parámetros válidos para guardar." });
        }

        try
        {
            string usuario = User?.Identity?.Name ?? "Usuario_Web";
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

            return Json(new { success = true, message = "Los límites y el motivo de cambio fueron registrados exitosamente." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Error en el servidor: {ex.Message}" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> ObtenerAuditoria(int? idMaquina, string? salida, DateTime? fechaInicio, DateTime? fechaFin)
    {
        if (!TienePermisoRecetas()) return Forbid();

        try
        {
            var maquinas = await _enharttService.ObtenerMaquinasAsync() ?? [];
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
        if (!TienePermisoRecetas()) return Forbid();

        if (dto == null || dto.IdMaquina <= 0 || dto.NumeroSalida <= 0)
        {
            return BadRequest(new { success = false, message = "Datos de máquina o salida no válidos." });
        }

        try
        {
            string usuario = User?.Identity?.Name ?? "Usuario_Web";
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
        if (!TienePermisoRecetas()) return Forbid();

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

            return Json(new { success = true, message = $"Celda '{nuevaMaquina.Celda}' creada exitosamente inicializada en la Salida {salidaInicial}." });
        }
        catch (Exception ex)
        {
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