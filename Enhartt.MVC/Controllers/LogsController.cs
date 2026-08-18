using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers
{
    [Authorize]
    public class LogsController : Controller
    {
        private readonly string _directorioLogs;
        private readonly IConfiguration _configuration;

        public LogsController(IConfiguration configuration)
        {
            _configuration = configuration;
            _directorioLogs = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
        }

        private bool EsSuperadmin()
        {
            // 1. Obtener usuario de red
            string nombreUsuario = User?.Identity?.Name ?? "";
            if (nombreUsuario.Contains('\\'))
            {
                nombreUsuario = nombreUsuario.Split('\\')[1];
            }

            // Respaldo local si no llega por handshake
            if (string.IsNullOrEmpty(nombreUsuario))
            {
                nombreUsuario = Environment.UserName;
            }

            // 2. Leer configuración desde PermisosSettings:Sistemas (o SuperadminSettings como fallback)
            var usuarios = _configuration.GetSection("PermisosSettings:Sistemas:Usuarios").Get<List<string>>()
                           ?? _configuration.GetSection("SuperadminSettings:UsuariosAutorizados").Get<List<string>>()
                           ?? new();

            if (usuarios.Any(u => u.Equals(nombreUsuario, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // 3. Validación por roles de Windows protegida
            try
            {
                var roles = _configuration.GetSection("PermisosSettings:Sistemas:Roles").Get<List<string>>()
                            ?? _configuration.GetSection("SuperadminSettings:RolesAutorizados").Get<List<string>>()
                            ?? new();

                return roles.Any(r => User.IsInRole(r));
            }
            catch
            {
                return false;
            }
        }

        public IActionResult Index()
        {
            if (!EsSuperadmin())
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
            if (!EsSuperadmin()) return Forbid();

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
        public IActionResult Descargar(string archivo)
        {
            if (!EsSuperadmin()) return Forbid();

            if (string.IsNullOrEmpty(archivo) || archivo.Contains(".."))
                return BadRequest("Nombre de archivo no permitido.");

            var ruta = Path.Combine(_directorioLogs, archivo);
            if (!System.IO.File.Exists(ruta)) return NotFound();

            var bytes = System.IO.File.ReadAllBytes(ruta);
            return File(bytes, "text/plain", archivo);
        }
    }
}