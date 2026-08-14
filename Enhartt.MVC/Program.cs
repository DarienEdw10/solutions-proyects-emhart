using Enhartt.Domain.Data;
using Enhartt.Domain.Repositories;
using Enhartt.MVC.Services;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Inyección de dependencias SQL Server
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
    builder.Configuration.GetConnectionString("DefaultConnection")));

// Inyección de dependencias de Repositorio y Servicios
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<EnharttService>();

// 1. Configuración de Autenticación de Windows
builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
    .AddNegotiate();

// Se habilita autorización SIN FallbackPolicy global
// (La autenticación se exigirá únicamente donde pongas [Authorize])
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// 2. Middlewares de Seguridad en orden correcto
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();