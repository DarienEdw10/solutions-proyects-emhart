using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Data;
using Enhartt.Domain.Repositories;
using Enhartt.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar DbContext desde la librería Enhartt.Domain
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSql")));

// 2. Registrar Inyección de Dependencias para Repositorios y Servicios (NO GENÉRICO)
builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IParametroService, ParametroService>();
builder.Services.AddScoped<IRecetaService, RecetaService>();

// 3. Configurar CORS para permitir peticiones desde la interfaz web
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PermitirTodo");
app.UseAuthorization();
app.MapControllers();

app.Run();