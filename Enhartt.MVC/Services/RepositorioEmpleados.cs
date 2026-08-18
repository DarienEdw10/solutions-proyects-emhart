using Magna.Cosma.Autotek.Autentificacion.Library;
using Magna.Cosma.Autotek.Log;

namespace Magna.Cosma.Autotek.VIPTRA.Foreign
{
    public class RepositorioEmpleados
    {
        private readonly RepositorioDB? repositorioDB;
        private readonly Logger fileLogger;

        public RepositorioEmpleados(SettingsAutentificacion settingsAutentificacion, Logger fileLogger)
        {
            this.fileLogger = fileLogger;
            try
            {
                repositorioDB = new RepositorioDB(settingsAutentificacion);
            }
            catch (Exception ex)
            {
                fileLogger.Registrar(
                    nivel: Logger.NivelesLog.Basico,
                    tipo: Logger.TiposLog.Errores,
                    origen: $"{GetType().Name}",
                    texto: $"Error al instanciar RepositorioDB: [{ex.Message}]");
                repositorioDB = null;
            }
        }

        public List<Empleado> ObtenerEmpleados(bool soloActivos = true)
        {
            if (repositorioDB == null) return new List<Empleado>();

            try
            {
                return repositorioDB.ObtenerEmpleados(soloActivos);
            }
            catch (Exception exception)
            {
                fileLogger.Registrar(
                    nivel: Logger.NivelesLog.Basico,
                    tipo: Logger.TiposLog.Errores,
                    origen: $"{GetType().Name}",
                    texto: $"Error en [{GetType().Name}.{fileLogger.ObtenerNombreDelMetodo()}] [{exception.Message}].");
                return new List<Empleado>();
            }
        }

        public Empleado? ObtenerEmpleadoPorCWID(string cwid)
        {
            if (repositorioDB == null) return null;

            try
            {
                Empleado emp = repositorioDB.ObtenerEmpleado(cwid);
                return emp != null && emp.Id > 0 ? emp : null;
            }
            catch (Exception exception)
            {
                fileLogger.Registrar(
                    nivel: Logger.NivelesLog.Basico,
                    tipo: Logger.TiposLog.Errores,
                    origen: $"{GetType().Name}",
                    texto: $"Error en [{GetType().Name}.{fileLogger.ObtenerNombreDelMetodo()}] [{exception.Message}].");
                return null;
            }
        }
    }
}