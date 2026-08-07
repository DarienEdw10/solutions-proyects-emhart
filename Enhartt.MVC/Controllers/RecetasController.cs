using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Controllers;

public class RecetasController : Controller
{
    // Carga la vista /Views/Recetas/recetas.cshtml
    public IActionResult Index()
    {
        return View("recetas");
    }

    // O si navegas directamente a /Recetas/Recetas
    public IActionResult Recetas()
    {
        return View();
    }
}