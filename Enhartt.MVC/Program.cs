using Enhartt.Domain.Data;
using Enhartt.Domain.Repositories;
using Enhartt.MVC.Services;
using Magna.Cosma.Autotek.Autentificacion.Library;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;
using Logger = Magna.Cosma.Autotek.Log.Logger;

var builder = WebApplication.CreateBuilder(args);

// =============================================================
// 1. CONFIGURACIÓN DEL LOGGER CORPORATIVO (Magna Autotek)
// =============================================================
Magna.Cosma.Autotek.Log.Settings logSettings = new();
builder.Configuration.GetSection("LogSettings").Bind(logSettings);

var directorioLogs = Path.Combine(Directory.GetCurrentDirectory(), "Logs");
if (!Directory.Exists(directorioLogs))
{
    Directory.CreateDirectory(directorioLogs);
}

// Configuración del archivo físico de Logs
string nombreArchivoLog = string.IsNullOrWhiteSpace(logSettings.ArchivoDeLog)
    ? "TuckerMonitor-00.log"
    : logSettings.ArchivoDeLog;

logSettings.ArchivoDeLog = Path.Combine(directorioLogs, Path.GetFileName(nombreArchivoLog));
Logger logger = new(logSettings);

// Inyección del logger corporativo como Singleton
builder.Services.AddSingleton(logger);

// =============================================================
// 2. CONFIGURACIÓN DE AUTENTICACIÓN Y REPOSITORIO DE EMPLEADOS
// =============================================================
SettingsAutentificacion settingsAutentificacion = new();
builder.Configuration.GetSection("SettingsAutentificacion").Bind(settingsAutentificacion);

// Inyección como Singleton para mantener el caché y evitar reconexiones lentas en cada petición HTTP
builder.Services.AddSingleton<RepositorioEmpleados>(sp =>
    new RepositorioEmpleados(settingsAutentificacion, logger));

// =============================================================
// 3. INYECCIÓN DE DEPENDENCIAS MVC Y BASE DE DATOS SQLITE
// =============================================================
builder.Services.AddControllersWithViews();

// Configuración de DbContext con SQLite
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? "Data Source=emhart_local.db";

// Resuelve la ruta si el archivo está en la raíz de la solución
if (connectionString.Contains("emhart_local.db") && !File.Exists("emhart_local.db") && File.Exists("../emhart_local.db"))
{
    connectionString = "Data Source=../emhart_local.db";
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlite(connectionString);
});

builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<EnharttService>();
builder.Services.AddHttpContextAccessor();

// Autenticación integrada de Windows
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// =============================================================
// 4. PIPELINE HTTP
// =============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// =============================================================
// MOCK DE IDENTIDAD PARA PRUEBAS (Hardcode temporal)
// =============================================================
/*app.Use(async (context, next) =>
{
    string cwidSimulado = "darienedwin.jimenez";

    var claims = new[]
    {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, cwidSimulado),
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, cwidSimulado)
    };

    var identity = new System.Security.Claims.ClaimsIdentity(claims, "PruebaMock");
    context.User = new System.Security.Claims.ClaimsPrincipal(identity);

    await next();
});
*/

app.UseAuthentication();
app.UseAuthorization();

// =============================================================
// AUDITORÍA DE INICIO DE SESIÓN Y ACCESOS WEB
// =============================================================
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value?.ToLower() ?? "";
    bool esRutaVista = path == "" || path == "/" || path.StartsWith("/home") || path.StartsWith("/recetas") || path.StartsWith("/logs");
    bool esLlamadaApiOEstatal = path.Contains(".") || path.Contains("obtener") || path.Contains("revalidar");

    if (esRutaVista && !esLlamadaApiOEstatal && context.User?.Identity?.IsAuthenticated == true)
    {
        string cwid = context.User.Identity.Name ?? "Desconocido";
        var log = context.RequestServices.GetService<Logger>();

        log?.Registrar(
            nivel: Logger.NivelesLog.Detallado,
            tipo: Logger.TiposLog.Informativo,
            origen: "Seguridad.Acceso",
            texto: $"El usuario [{cwid}] inició sesión / ingresó a la vista [{path}].");
    }

    await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var repoEmpleados = scope.ServiceProvider.GetRequiredService<RepositorioEmpleados>();
    _ = repoEmpleados.PrecargarPadronAsync();
}

app.Run();