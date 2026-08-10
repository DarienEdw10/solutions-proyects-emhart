using Enhartt.Domain.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Enhartt.Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MaquinasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MaquinasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/maquinas
        // Retorna el catálogo de Celdas registradas
        [HttpGet]
        public async Task<IActionResult> GetCeldas()
        {
            var maquinasRaw = await _context.Maquinas
                                            .AsNoTracking()
                                            .ToListAsync();

            var maquinas = maquinasRaw.Select(m => {
                // Parseo seguro por si viene en string "1", "3" o "CELDA-01"
                int.TryParse(m.IdMaquina, out int idNum);
                
                string nombre = (m.IdMaquina == "3" || idNum == 3) ? "CELDA-03" 
                              : (idNum > 0 ? $"CELDA-{idNum:D2}" : m.IdMaquina);

                return new
                {
                    idMaquina = m.IdMaquina,
                    nombreCelda = nombre
                };
            }).ToList();

            return Ok(maquinas);
        }

        // GET: api/maquinas/salidas?maquina=1
        // Retorna las salidas/pistolas únicas registradas para una celda específica
        [HttpGet("salidas")]
        public async Task<IActionResult> GetSalidas([FromQuery] int maquina)
        {
            var salidas = await _context.Parametros
                                        .AsNoTracking()
                                        .Where(p => p.IdentificadorId == maquina && p.Salida.HasValue)
                                        .Select(p => p.Salida!.Value)
                                        .Distinct()
                                        .OrderBy(s => s)
                                        .ToListAsync();

            if (!salidas.Any())
            {
                salidas = new List<int> { 1, 2, 3 };
            }

            var resultado = salidas.Select(s => new {
                valor = s,
                texto = $"Out {s}"
            });

            return Ok(resultado);
        }
    }
}