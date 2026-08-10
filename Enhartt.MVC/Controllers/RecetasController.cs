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

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return await CargarVistaRecetasAsync();
    }

    [HttpGet]
    public async Task<IActionResult> Recetas()
    {
        return await CargarVistaRecetasAsync();
    }

    private async Task<IActionResult> CargarVistaRecetasAsync()
    {
        var maquinas = await enharttService.ObtenerMaquinasAsync() ?? [];
        
        var celdas = maquinas
            .Where(m => !string.IsNullOrEmpty(m.Celda))
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
                // Obtenemos los registros COMPLETOS de la tabla de recetas (con MinVal, MaxVal, Parametro, etc.)
                var recetasDb = await repository.ObtenerRecetasPorMaquinaAsync(maquina.Id) ?? [];

                maquinasVM.Add(new MaquinaViewModel
                {
                    Id = maquina.Id,
                    IdMaquina = maquina.IdMaquina ?? "",
                    Recetas = recetasDb.Select(r => new RecetaViewModel
                    {
                        Id = r.IdReferencia,
                        Salida = r.Salida.ToString(),
                        Parametro = r.Parametro ?? "Límite Control",
                        MinVal = r.MinVal,
                        MaxVal = r.MaxVal,
                        FechaModificacion = r.FechaModificacion ?? r.FechaCreacion,
                        ModificadoPor = r.ModificadoPor ?? "SISTEMA_INICIAL",
                        Estado = r.Estado
                    }).ToList()
                });
            }

            celdasVM.Add(new CeldaViewModel
            {
                Celda = celdaKVP.Key,
                Maquinas = maquinasVM
            });
        }

        return View("recetas", new RecetasViewModel()
        {
            Celdas = celdasVM
        });
    }
}