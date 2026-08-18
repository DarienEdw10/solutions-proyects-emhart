using Enhartt.Domain.Data;
using Enhartt.Domain.Repositories;
using Enhartt.MVC.Services;
using Magna.Cosma.Autotek.Autentificacion.Library;
using Magna.Cosma.Autotek.VIPTRA.Foreign;
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

logSettings.ArchivoDeLog = Path.Combine(directorioLogs, logSettings.ArchivoDeLog);
Logger logger = new(logSettings);

// Log de arranque de la aplicación
logger.Registrar(
    nivel: Logger.NivelesLog.Detallado,
    tipo: Logger.TiposLog.Informativo,
    origen: "Program.cs",
    texto: "Inicio del servicio web [Magna.Cosma.Autotek.TuckerMonitor]");

// Inyección del logger corporativo
builder.Services.AddSingleton(logger);

// =============================================================
// 2. CONFIGURACIÓN DE AUTENTICACIÓN Y REPOSITORIO DE EMPLEADOS
// =============================================================
SettingsAutentificacion settingsAutentificacion = new();
builder.Configuration.GetSection("SettingsAutentificacion").Bind(settingsAutentificacion);

// Inyección del Repositorio de Empleados mediante ActivatorUtilities
builder.Services.AddScoped(service => ActivatorUtilities.CreateInstance<RepositorioEmpleados>(
    service,
    settingsAutentificacion,
    logger
));

// =============================================================
// 3. INYECCIÓN DE DEPENDENCIAS MVC Y BASE DE DATOS
// =============================================================
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();