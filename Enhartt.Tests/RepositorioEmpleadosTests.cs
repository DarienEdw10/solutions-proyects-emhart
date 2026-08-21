using Enhartt.MVC.Services;
using Magna.Cosma.Autotek.Autentificacion.Library;
using Magna.Cosma.Autotek.Log;
using Logger = Magna.Cosma.Autotek.Log.Logger;
using Xunit;

namespace Enhartt.Tests;

public class RepositorioEmpleadosTests
{
    // Usar la referencia explícita del servicio de MVC
    private readonly Enhartt.MVC.Services.RepositorioEmpleados _repositorio;

    public RepositorioEmpleadosTests()
    {
        var settings = new SettingsAutentificacion
        {
            Servidor = @"atk1app47\atkapps01",
            BaseDeDatos = "atkAutentificacion",
            Usuario = "Autotek.Tareas",
            Password = "Autotek.Tareas",
            UsarCredencialesBD = true
        };

        string rutaLogTemporal = Path.Combine(Path.GetTempPath(), "test_autotek.log");
        var logSettings = new Settings { ArchivoDeLog = rutaLogTemporal };
        var logger = new Logger(logSettings);

        _repositorio = new Enhartt.MVC.Services.RepositorioEmpleados(settings, logger);
    }
    [Fact]
    public void BuscarMiUsuarioEnPadron()
    {
        // 1. Obtener todos los empleados activos del padrón
        var empleados = _repositorio.ObtenerEmpleados(soloActivos: true);

        // 2. Filtrar por nombre
        var misRegistros = empleados
            .Where(e => (e.NombrePropio != null && e.NombrePropio.Contains("Darien Edwin", StringComparison.OrdinalIgnoreCase))
                     || (e.ApellidoPaterno != null && e.ApellidoPaterno.Contains("Jimenez", StringComparison.OrdinalIgnoreCase))
                     || (e.ApellidoMaterno != null && e.ApellidoMaterno.Contains("Gonzaga", StringComparison.OrdinalIgnoreCase))
                     || (e.NombrePorApellidos != null && e.NombrePorApellidos.Contains("Jimenez Gonzaga Darien Edwin", StringComparison.OrdinalIgnoreCase)))
            .ToList();

        // Assert
        Assert.NotEmpty(misRegistros);
    }

    [Fact]
    public void ObtenerEmpleadoPorCWID_DebeRetornarDarienEdwin()
    {
        // Act
        var empleado = _repositorio.ObtenerEmpleadoPorCWID("darijim1");

        // Assert
        Assert.NotNull(empleado);
        Assert.Equal("Darien Edwin Jimenez Gonzaga", empleado.NombrePropio);
        Assert.Equal(32352, empleado.NumeroDeEmpleado);
    }
}