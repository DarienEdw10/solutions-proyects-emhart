using Enhartt.MVC.Models.ViewModels;
using Enhartt.MVC.Services;
using Enhartt.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers;

public class RecetasController : Controller
{
    private readonly EnharttService enharttService;
    private readonly IRepository repository;

    public RecetasController(EnharttService enharttService, IRepository repository)
    {
        this.enharttService = enharttService;
        this.repository = repository;
    }

    public async Task<IActionResult> Index()
    {
        var maquinas = await enharttService.ObtenerMaquinasAsync();
        
        var celdas = maquinas
            .Where(m => m.Celda != null)
            .GroupBy(m => m.Celda!)
            .ToDictionary(
                grp => grp.Key,
                grp => grp.ToList()
            );

        List<CeldaViewModel> celdasVM = [];

        foreach (var celdaKVP in celdas)
        {
            List<MaquinaViewModel> maquinasVM = [];

            foreach (var maquina in celdaKVP.Value)
            {
                // Obtener las salidas registradas para esta máquina concreta
                var salidas = await repository.ObtenerSalidasPorMaquinaAsync(maquina.Id);

                maquinasVM.Add(new MaquinaViewModel
                {
                    Id = maquina.Id,
                    IdMaquina = maquina.IdMaquina,
                    Recetas = salidas.Select(s => new RecetaViewModel
                    {
                        Id = maquina.Id,
                        Salida = s.ToString()
                    }).ToList()
                });
            }

            CeldaViewModel celdaVM = new()
            {
                Celda = celdaKVP.Key,
                Maquinas = maquinasVM
            };

            celdasVM.Add(celdaVM);
        }

        return View("recetas", new RecetasViewModel()
        {
            Celdas = celdasVM
        });
    }

    public IActionResult Recetas()
    {
        return View("recetas");
    }
}