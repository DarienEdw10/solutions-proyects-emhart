using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Models;
using Enhartt.Infrastructure.Data;

namespace Enhartt.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ParametrosController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ParametrosController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetParametros()
        {
            try
            {
                var resultado = await (from p in _context.Parametros.AsNoTracking()
                                       join m in _context.Maquinas.AsNoTracking() 
                                       on p.IdentificadorId equals m.Id into joinMaq
                                       from m in joinMaq.DefaultIfEmpty()
                                       orderby p.IdentificadorId descending
                                       select new
                                       {
                                           fecha = p.Fecha.HasValue ? p.Fecha.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                                           celda = m != null ? m.IdMaquina.ToUpper() : "CELDA-01",
                                           salida = $"Out {p.Salida ?? 1}",
                                           programa = p.Programa ?? 1,
                                           numSol = p.NumSol ?? 0,
                                           corriente = p.Corriente ?? 0,
                                           energia = p.Energia ?? 0,
                                           tiempo = p.Tiempo ?? 0,
                                           penetracion = p.Penetracion ?? 0,
                                           volArc = p.VolArc ?? 0,
                                           volPri = p.VolPri ?? 0,
                                           elevacion = p.Elevacion ?? 0,
                                           caida = p.Caida ?? 0,
                                           lonPer = p.LonPer ?? 0,
                                           estatus = (p.EstatusCalidad ?? "OK").ToUpper() == "OK" ? "OK" : "NOK",
                                           detalles = p.DetallesFallas ?? "",
                                           turno = "Turno 1"
                                       })
                                       .Take(100)
                                       .ToListAsync();

                return Ok(resultado);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}