using Microsoft.EntityFrameworkCore;
using Enhartt.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Configurar DbContext apuntando al proyecto Infrastructure con la cadena de appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSql")));

// 2. Configurar CORS para permitir peticiones desde la interfaz web (Live Server)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PermitirTodo", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 3. Registrar los controladores
builder.Services.AddControllers();

// 4. Configurar Swagger UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("PermitirTodo");
app.UseAuthorization();

// Mapear los Controllers (como ParametrosController)
app.MapControllers();

app.Run();