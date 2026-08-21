using Enhartt.Domain.Data;
using Enhartt.Domain.Models;
using Enhartt.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Enhartt.Tests;

public class RepositoryTests
{
    private async Task<AppDbContext> ObtenerContextoEnMemoriaAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);

        // Sembrar datos de prueba
        context.Usuarios.AddRange(
            new Usuario { CWID = "darijim1", NivelDeUsuario = 30, Activo = true },
            new Usuario { CWID = "operador01", NivelDeUsuario = 10, Activo = true },
            new Usuario { CWID = "inactivo01", NivelDeUsuario = 20, Activo = false }
        );

        await context.SaveChangesAsync();
        return context;
    }

    [Fact]
    public async Task ObtenerNivelUsuarioPorCWIDAsync_UsuarioValido_DebeRetornarNivel()
    {
        // Arrange
        using var context = await ObtenerContextoEnMemoriaAsync();
        var repo = new Repository(context);

        // Act
        int nivelDirecto = await repo.ObtenerNivelUsuarioPorCWIDAsync("darijim1");
        int nivelConDominio = await repo.ObtenerNivelUsuarioPorCWIDAsync("AUTOTEK\\darijim1");

        // Assert (Pon aquí el punto rojo para depurar)
        Assert.Equal(30, nivelDirecto);
        Assert.Equal(30, nivelConDominio);
    }

    [Fact]
    public async Task ObtenerNivelUsuarioPorCWIDAsync_UsuarioInactivoOInexistente_DebeRetornarCero()
    {
        // Arrange
        using var context = await ObtenerContextoEnMemoriaAsync();
        var repo = new Repository(context);

        // Act
        int nivelInactivo = await repo.ObtenerNivelUsuarioPorCWIDAsync("inactivo01");
        int nivelNoExiste = await repo.ObtenerNivelUsuarioPorCWIDAsync("usuario_fantasma");
        int nivelVacio = await repo.ObtenerNivelUsuarioPorCWIDAsync("");

        // Assert
        Assert.Equal(0, nivelInactivo);
        Assert.Equal(0, nivelNoExiste);
        Assert.Equal(0, nivelVacio);
    }
}