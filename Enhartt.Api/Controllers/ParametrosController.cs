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

        [HttpGet]
        public async Task<IActionResult> GetParametros([FromQuery] int pagina = 1, [FromQuery] int tamano = 5)
        {
            try
            {
                var resultado = await _parametroService.ObtenerParametrosPaginadosAsync(pagina, tamano);
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