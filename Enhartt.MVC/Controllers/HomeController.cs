using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Enhartt.MVC.Models;

namespace Enhartt.MVC.Controllers;

public class HomeController : Controller
{
    // GET: / (Dashboard de Monitoreo Tucker)
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}