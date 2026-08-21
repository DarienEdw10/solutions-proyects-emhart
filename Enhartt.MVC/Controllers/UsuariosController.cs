using Enhartt.Domain.Repositories;
using Enhartt.MVC.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Logger = Magna.Cosma.Autotek.Log.Logger;

namespace Enhartt.MVC.Controllers;

[Authorize]
public class UsuariosController : Controller
{
    private readonly IRepository _repository;
    private readonly RepositorioEmpleados _repositorioEmpleados;
    private readonly Logger _logger;

    public UsuariosController(
        IRepository repository,
        RepositorioEmpleados repositorioEmpleados,
        Logger logger)
    {
        _repository = repository;
        _repositorioEmpleados = repositorioEmpleados;
        _logger = logger;
    }

    private async Task<bool> EsSistemasAsync()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;

        string cwid = User.Identity?.Name ?? "";
        if (cwid.Contains('\\')) cwid = cwid.Split('\\')[1];
        if (string.IsNullOrEmpty(cwid)) cwid = Environment.UserName;

        try
        {
            int nivel = await _repository.ObtenerNivelUsuarioPorCWIDAsync(cwid);
            return nivel >= 30;
        }
        catch
        {
            return false;
        }
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!await EsSistemasAsync()) return Forbid();
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> BuscarColaboradoresCorporativos(string? query)
    {
        if (!await EsSistemasAsync()) return Forbid();

        try
        {
            var empleados = _repositorioEmpleados.ObtenerEmpleados(soloActivos: true);
            string q = query?.Trim().ToUpper() ?? "";

            var filtrados = empleados
                .Where(e => e != null)
                .Select(e =>
                {
                    string cwidReal = "";

                    // 1. Extraer desde la colección CWIDs del empleado
                    if (e.CWIDs != null)
                    {
                        foreach (var c in e.CWIDs)
                        {
                            if (c == null) continue;

                            var propValor = c.GetType().GetProperty("Valor")?.GetValue(c)?.ToString()
                                         ?? c.GetType().GetProperty("Cuenta")?.GetValue(c)?.ToString()
                                         ?? c.GetType().GetProperty("Nombre")?.GetValue(c)?.ToString()
                                         ?? c.GetType().GetProperty("CWID")?.GetValue(c)?.ToString();

                            string raw = propValor ?? c.ToString() ?? "";

                            // Descartar namespaces de la DLL y hashes largos (>= 30 chars)
                            if (!string.IsNullOrWhiteSpace(raw) &&
                                !raw.Contains("Magna.Cosma.Autotek") &&
                                raw.Length < 30)
                            {
                                cwidReal = raw.Trim();
                                break;
                            }
                        }
                    }

                    // 2. Respaldo desde el correo corporativo (@magna.com)
                    if (string.IsNullOrWhiteSpace(cwidReal) && e.Correos != null)
                    {
                        var correo = e.Correos.FirstOrDefault(corr => corr != null && !string.IsNullOrWhiteSpace(corr.Direccion));
                        if (correo != null && correo.Direccion.Contains('@'))
                        {
                            string prefijo = correo.Direccion.Split('@')[0].Trim();
                            if (prefijo.Length < 30)
                            {
                                cwidReal = prefijo;
                            }
                        }
                    }

                    string nombreCompleto = !string.IsNullOrWhiteSpace(e.NombrePropio)
                        ? e.NombrePropio.Trim()
                        : (e.NombrePorApellidos ?? $"{e.Nombre} {e.ApellidoPaterno}".Trim());

                    return new
                    {
                        numero = e.NumeroDeEmpleado,
                        nombre = nombreCompleto,
                        planta = e.Planta.ToString(),
                        cwid = cwidReal
                    };
                })
                .Where(e => string.IsNullOrEmpty(q)
                    || e.numero.ToString().Contains(q)
                    || e.nombre.ToUpper().Contains(q)
                    || (!string.IsNullOrEmpty(e.cwid) && e.cwid.ToUpper().Contains(q)))
                .Take(30)
                .ToList();

            return Json(new { success = true, data = filtrados });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> AsignarNivel([FromBody] AsignarNivelDto dto)
    {
        if (!await EsSistemasAsync()) return Forbid();

        if (dto == null || string.IsNullOrWhiteSpace(dto.Cwid) || dto.Nivel < 10)
        {
            return BadRequest(new { success = false, message = "Datos inválidos para asignación de nivel." });
        }

        string usuarioActual = User?.Identity?.Name ?? "Sistemas";

        try
        {
            await _repository.GuardarUsuarioNivelAsync(dto.Cwid.Trim(), dto.Nivel, dto.Nombre, usuarioActual);

            _logger.Registrar(
                nivel: Logger.NivelesLog.Detallado,
                tipo: Logger.TiposLog.Informativo,
                origen: "UsuariosController.AsignarNivel",
                texto: $"El usuario [{usuarioActual}] asignó Nivel [{dto.Nivel}] al colaborador [{dto.Nombre}] (CWID: [{dto.Cwid}]).");

            return Json(new { success = true, message = $"Permisos actualizados para {dto.Nombre}." });
        }
        catch (Exception ex)
        {
            string detalleError = ex.InnerException != null ? ex.InnerException.Message : ex.Message;

            _logger.Registrar(
                nivel: Logger.NivelesLog.Basico,
                tipo: Logger.TiposLog.Errores,
                origen: "UsuariosController.AsignarNivel",
                texto: $"Error al asignar nivel a [{dto.Cwid}]: {detalleError}");

            return StatusCode(500, new { success = false, message = detalleError });
        }
    }
}

public class AsignarNivelDto
{
    public string Cwid { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public int Nivel { get; set; }
}