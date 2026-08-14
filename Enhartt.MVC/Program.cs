using Enhartt.Domain.Data;
using Enhartt.Domain.Repositories;
using Enhartt.MVC.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Inyeccion de dependencias sqlserver
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyeccion de dependencias con respecto al repositorio
builder.Services.AddScoped<IRepository, Repository>();

// Inyeccion de dependencias con respecto al servicio
builder.Services.AddScoped<EnharttService>();

// 1. Configuración de Autenticación de Windows (Directorio Activo / Sesión de PC)
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

builder.Services.AddAuthorization(options =>
{
    // Por defecto autentica las peticiones
    options.FallbackPolicy = options.DefaultPolicy;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 2. Middlewares de Seguridad (¡UseAuthentication SIEMPRE va antes de UseAuthorization!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();