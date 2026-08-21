using Enhartt.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly string _directorioLogs;
        private readonly IConfiguration _configuration;
        private readonly IRepository _repository;

        public LogsController(IConfiguration configuration, IRepository repository)
        {
            _configuration = configuration;
            _repository = repository;
            _directorioLogs = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        }

        private async Task<bool> EsSuperadminAsync()
        {
            // 1. Obtener usuario de red
            string nombreUsuario = User?.Identity?.Name ?? "";
            if (nombreUsuario.Contains('\\'))
            {
                nombreUsuario = nombreUsuario.Split('\\')[1];
            }

            if (string.IsNullOrEmpty(nombreUsuario))
            {
                nombreUsuario = Environment.UserName;
            }

            // 2. Consulta en base de datos emhart.usuarios (Nivel >= 30)
            try
            {
                int nivelUsuario = await _repository.ObtenerNivelUsuarioPorCWIDAsync(nombreUsuario);
                if (nivelUsuario >= 30)
                {
                    return true;
                }
            }
            catch { }

            // 3. Fallback en appsettings.json
            var usuarios = _configuration.GetSection("PermisosSettings:Sistemas:Usuarios").Get<List<string>>()
                           ?? _configuration.GetSection("SuperadminSettings:UsuariosAutorizados").Get<List<string>>()
                           ?? new();

            if (usuarios.Any(u => u.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 4. Validación por roles de Windows protegida
            try
            {
                var roles = _configuration.GetSection("PermisosSettings:Sistemas:Roles").Get<List<string>>()
                            ?? _configuration.GetSection("SuperadminSettings:RolesAutorizados").Get<List<string>>()
                            ?? new();

                return roles.Any(r => User?.IsInRole(r) == true);
            }
            catch
            {
                return false;
            }
        }

        public async Task<IActionResult> Index()
        {
            if (!await EsSuperadminAsync())
            {
                return Forbid();
            }

            if (!Directory.Exists(_directorioLogs))
            {
                Directory.CreateDirectory(_directorioLogs);
            }

            var archivos = Directory.GetFiles(_directorioLogs, "*.log*")
                                    .Select(f => new FileInfo(f))
                                    .OrderByDescending(f => f.LastWriteTime)
                                    .Select(f => new
                                    {
                                        Nombre = f.Name,
                                        TamañoKb = Math.Round(f.Length / 1024.0, 2),
                                        UltimaModificacion = f.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                                    })
                                    .ToList();

            ViewBag.Archivos = archivos;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> LeerContenidoLog(string archivo)
        {
            if (!await EsSuperadminAsync()) return Forbid();

            try
            {
                if (string.IsNullOrEmpty(archivo) || archivo.Contains(".."))
                    return BadRequest("Nombre de archivo inválido.");

                var rutaCompleta = Path.Combine(_directorioLogs, archivo);
                if (!System.IO.File.Exists(rutaCompleta))
                    return NotFound("El archivo no existe.");

                using var fs = new FileStream(rutaCompleta, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var contenido = await sr.ReadToEndAsync();

                return Json(new { success = true, contenido = contenido });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> Descargar(string archivo)
        {
            if (!await EsSuperadminAsync()) return Forbid();

            if (string.IsNullOrEmpty(archivo) || archivo.Contains(".."))
                return BadRequest("Nombre de archivo no permitido.");

            var ruta = Path.Combine(_directorioLogs, archivo);
            if (!System.IO.File.Exists(ruta)) return NotFound();

            var bytes = await System.IO.File.ReadAllBytesAsync(ruta);
            return File(bytes, "text/plain", archivo);
        }
    }
}