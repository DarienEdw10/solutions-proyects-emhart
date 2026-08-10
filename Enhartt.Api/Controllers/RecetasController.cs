using Microsoft.AspNetCore.Mvc;
using Enhartt.Api.Services;
using Enhartt.Domain.Models;

namespace Enhartt.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RecetasController : ControllerBase
    {
        private readonly IRecetaService _recetaService;

        public RecetasController(IRecetaService recetaService)
        {
            _recetaService = recetaService;
        }

        // GET: api/recetas?maquina=1&salida=3
        // Obtiene las recetas VIGENTES / ACTIVAS (estado = true)
        [HttpGet]
        public async Task<IActionResult> GetRecetaActiva([FromQuery] int maquina, [FromQuery] int salida)
        {
            try
            {
                var datos = await _recetaService.ObtenerRecetaActivaAsync(maquina, salida);
                return Ok(datos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener la receta activa: {ex.Message}" });
            }
        }

        // POST: api/recetas/guardar
        // Inhabilita la versión anterior (estado = false) e inserta los nuevos límites activos (estado = true)
        [HttpPost("guardar")]
        public async Task<IActionResult> Guardar([FromBody] GuardarRecetaRequest request)
        {
            if (request == null || request.Parametros == null || !request.Parametros.Any())
            {
                return BadRequest(new { error = "La petición no contiene parámetros válidos para actualizar." });
            }

            try
            {
                await _recetaService.ActualizarRecetaConHistorialAsync(
                    request.IdMaquina, 
                    request.Salida, 
                    request.Parametros, 
                    request.Usuario
                );

                return Ok(new { mensaje = "Receta actualizada y versión anterior inhabilitada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al guardar la receta: {ex.Message}" });
            }
        }

        // GET: api/recetas/historial?maquina=1&fechaInicio=2026-08-01&fechaFin=2026-08-06
        // Recupera la auditoría histórica (registros activos e inhabilitados) filtrados por rango de fecha
        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorial(
            [FromQuery] int maquina, 
            [FromQuery] DateTime? fechaInicio, 
            [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var historial = await _recetaService.ObtenerHistorialCambiosAsync(maquina, fechaInicio, fechaFin);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Error al obtener el historial de auditoría: {ex.Message}" });
            }
        }
    }

    // DTO de petición para el payload JSON recibido desde JS
    public class GuardarRecetaRequest
    {
        public int IdMaquina { get; set; }
        public int Salida { get; set; }
        public string Usuario { get; set; } = "Usuario_Web";
        public List<Receta> Parametros { get; set; } = new();
    }
}