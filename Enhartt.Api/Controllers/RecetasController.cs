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

        [HttpPost("guardar")]
        public async Task<IActionResult> Guardar([FromBody] GuardarRecetaRequest request)
        {
            try
            {
                await _recetaService.GuardarCambiosRecetaAsync(request.Celda, request.Salida, request.Cambios);
                return Ok(new { mensaje = "Historial registrado correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("historial")]
        public async Task<IActionResult> GetHistorial([FromQuery] string? celda, [FromQuery] DateTime? fechaInicio, [FromQuery] DateTime? fechaFin)
        {
            try
            {
                var historial = await _recetaService.ObtenerHistorialAsync(celda, fechaInicio, fechaFin);
                return Ok(historial);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class GuardarRecetaRequest
    {
        public string Celda { get; set; } = string.Empty;
        public string Salida { get; set; } = string.Empty;
        public List<HistorialReceta> Cambios { get; set; } = new();
    }
}