using Microsoft.AspNetCore.Mvc;
using Enhartt.Api.Services;

namespace Enhartt.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParametrosController : ControllerBase
    {
        private readonly IParametroService _parametroService;

        public ParametrosController(IParametroService parametroService)
        {
            _parametroService = parametroService;
        }

        // GET: api/parametros?pagina=1&tamano=5&celda=CELDA-03&estatus=Solo OK&busqueda=7543
        [HttpGet]
        public async Task<IActionResult> GetParametros(
            [FromQuery] int pagina = 1, 
            [FromQuery] int tamano = 5,
            [FromQuery] string? celda = null,
            [FromQuery] string? estatus = null,
            [FromQuery] string? turno = null,
            [FromQuery] string? busqueda = null)
        {
            try
            {
                var resultado = await _parametroService.ObtenerParametrosPaginadosAsync(pagina, tamano, celda, estatus, turno, busqueda);
                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("kpis")]
        public async Task<IActionResult> GetKpis()
        {
            try
            {
                var kpis = await _parametroService.ObtenerResumenKpisAsync();
                return Ok(kpis);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}