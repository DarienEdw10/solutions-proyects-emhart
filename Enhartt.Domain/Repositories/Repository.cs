using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Data;
using Enhartt.Domain.Models;

namespace Enhartt.Domain.Repositories
{
    public class Repository : IRepository
    {
        protected readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<int> ObtenerNivelUsuarioPorCWIDAsync(string cwid)
        {
            if (string.IsNullOrWhiteSpace(cwid)) return 0;

            if (cwid.Contains('\\'))
            {
                cwid = cwid.Split('\\')[1];
            }

            var usuario = await _context.Usuarios
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.CWID.ToLower() == cwid.ToLower() && u.Activo);

            return usuario?.NivelDeUsuario ?? 0;
        }

        // =============================================================
        // RECETAS
        // =============================================================
        public async Task<IEnumerable<Receta>> ObtenerRecetasPorMaquinaAsync(int idMaquina)
        {
            return await _context.Receta
                                 .AsNoTracking()
                                 .Where(receta => receta.IdMaquina == idMaquina)
                                 .ToListAsync();
        }

        public async Task<IEnumerable<Receta>> ObtenerTodasLasRecetasActivasAsync()
        {
            return await _context.Receta
                                 .AsNoTracking()
                                 .Where(r => r.Estado == true)
                                 .ToListAsync();
        }

        public async Task ActualizarRecetaConHistorialAsync(Receta nuevaReceta, string usuario)
        {
            string paramLimpio = nuevaReceta.Parametro?.Trim() ?? "";

            var recetasAnteriores = await _context.Receta
                .Where(r => r.IdMaquina == nuevaReceta.IdMaquina &&
                            r.Salida == nuevaReceta.Salida &&
                            r.Parametro != null &&
                            r.Parametro.Trim() == paramLimpio &&
                            r.Estado == true)
                .ToListAsync();

            foreach (var recetaVieja in recetasAnteriores)
            {
                recetaVieja.Estado = false;
                recetaVieja.FechaModificacion = DateTime.Now;
                recetaVieja.ModificadoPor = usuario;
            }

            nuevaReceta.Estado = true;
            nuevaReceta.FechaCreacion = DateTime.Now;
            nuevaReceta.ModificadoPor = usuario;
            nuevaReceta.Comentario = string.IsNullOrWhiteSpace(nuevaReceta.Comentario)
                ? "Ajuste operativo de parámetros"
                : nuevaReceta.Comentario.Trim();

            await _context.Receta.AddAsync(nuevaReceta);
            await _context.SaveChangesAsync();
        }

        public async Task<Receta?> ActualizarRecetaAsync(Receta receta, string usuario)
        {
            Receta? recetaBd = null;

            if (receta.IdReferencia > 0)
            {
                recetaBd = await _context.Receta.FirstOrDefaultAsync(r => r.IdReferencia == receta.IdReferencia);
            }

            if (recetaBd == null)
            {
                recetaBd = await _context.Receta.FirstOrDefaultAsync(r =>
                    r.IdMaquina == receta.IdMaquina &&
                    r.Salida == receta.Salida &&
                    r.Parametro != null && r.Parametro.Trim() == receta.Parametro.Trim());
            }

            if (recetaBd == null) return null;

            recetaBd.MinVal = receta.MinVal;
            recetaBd.MaxVal = receta.MaxVal;
            recetaBd.Estado = receta.Estado;
            recetaBd.ModificadoPor = usuario;
            recetaBd.Comentario = receta.Comentario;
            recetaBd.FechaModificacion = DateTime.Now;

            _context.Receta.Update(recetaBd);
            await _context.SaveChangesAsync();
            return recetaBd;
        }

        public async Task<Receta?> AgregarRecetaAsync(Receta receta, string usuario)
        {
            receta.Estado = true;
            receta.FechaCreacion = DateTime.Now;
            receta.ModificadoPor = usuario;
            receta.Comentario = string.IsNullOrWhiteSpace(receta.Comentario)
                ? "Alta inicial de parámetro"
                : receta.Comentario.Trim();

            var entityEntry = await _context.Receta.AddAsync(receta);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        public async Task<IEnumerable<Receta>> ObtenerAuditoriaRecetasAsync(int? idMaquina, string? salida, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var query = _context.Receta
                                 .AsNoTracking()
                                 .AsQueryable();

            if (idMaquina.HasValue && idMaquina.Value > 0)
            {
                query = query.Where(r => r.IdMaquina == idMaquina.Value);
            }

            if (!string.IsNullOrEmpty(salida) && int.TryParse(salida, out int numSalida))
            {
                query = query.Where(r => r.Salida == numSalida);
            }

            if (fechaInicio.HasValue)
            {
                var inicio = fechaInicio.Value.Date;
                query = query.Where(r => (r.FechaModificacion ?? r.FechaCreacion) >= inicio);
            }

            if (fechaFin.HasValue)
            {
                var fin = fechaFin.Value.Date.AddDays(1);
                query = query.Where(r => (r.FechaModificacion ?? r.FechaCreacion) < fin);
            }

            return await query.OrderByDescending(r => r.FechaModificacion ?? r.FechaCreacion)
                              .ToListAsync();
        }

        // =============================================================
        // MAQUINAS
        // =============================================================
        public async Task<IEnumerable<Maquina>> ObtenerMaquinasAsync(bool soloActivos = true)
        {
            var query = _context.Maquinas
                                 .AsNoTracking()
                                 .AsQueryable();

            if (soloActivos) query = query.Where(maquina => maquina.Activo == true);

            return await query.ToListAsync();
        }

        public async Task<Maquina?> ObtenerMaquinaPorIdAsync(string idMaquina)
        {
            return await _context.Maquinas
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(m => m.IdMaquina == idMaquina);
        }

        public async Task<Maquina?> AgregarMaquinaAsync(Maquina maquina)
        {
            maquina.Activo = true;
            maquina.FechaCreacion = DateTime.Now;
            var entityEntry = await _context.Maquinas.AddAsync(maquina);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        public async Task<Maquina?> ActualizarMaquinaAsync(Maquina maquina)
        {
            Maquina? maquinaBd = await _context.Maquinas.FirstOrDefaultAsync(m => m.Id == maquina.Id || m.IdMaquina == maquina.IdMaquina);

            if (maquinaBd == null) return null;

            maquinaBd.Planta = maquina.Planta;
            maquinaBd.Linea = maquina.Linea;
            maquinaBd.Celda = maquina.Celda;
            maquinaBd.Estacion = maquina.Estacion;
            maquinaBd.Modelo = maquina.Modelo;
            maquinaBd.Activo = maquina.Activo;
            maquinaBd.FechaModificacion = DateTime.Now;

            var entityEntry = _context.Maquinas.Update(maquinaBd);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        // =============================================================
        // PARAMETROS (Telemetría de Soldaduras)
        // =============================================================
        private IQueryable<Parametro> ConstruirFiltroParametros(
            int? identificadorId,
            string? estatus,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? busqueda)
        {
            var query = _context.Parametros.AsNoTracking().AsQueryable();

            if (identificadorId.HasValue && identificadorId.Value > 0)
                query = query.Where(parametro => parametro.IdentificadorId == identificadorId.Value);

            if (!string.IsNullOrEmpty(estatus))
                query = query.Where(parametro => parametro.EstatusCalidad == estatus);

            if (fechaInicio.HasValue)
                query = query.Where(parametro => parametro.Fecha >= fechaInicio.Value.Date);

            if (fechaFin.HasValue)
            {
                var finLimite = fechaFin.Value.Date.AddDays(1);
                query = query.Where(parametro => parametro.Fecha < finLimite);
            }

            if (!string.IsNullOrEmpty(busqueda))
            {
                string term = busqueda.Trim();
                bool esNumero = int.TryParse(term, out int numBusqueda);

                query = query.Where(parametro =>
                    (esNumero && parametro.NumSol == numBusqueda) ||
                    (parametro.DetallesFallas != null && parametro.DetallesFallas.Contains(term)) ||
                    (parametro.Linea != null && parametro.Linea.Contains(term)));
            }

            return query;
        }

        public async Task<IEnumerable<Parametro>> ObtenerParametrosPaginadosAsync(
            int? identificadorId,
            string? estatus,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? busqueda,
            int pagina,
            int registrosPorPagina)
        {
            var query = ConstruirFiltroParametros(identificadorId, estatus, fechaInicio, fechaFin, busqueda);

            return await query.OrderByDescending(parametro => parametro.IdRegistro)
                              .Skip((pagina - 1) * registrosPorPagina)
                              .Take(registrosPorPagina)
                              .ToListAsync();
        }

        public async Task<(int totalDisparos, int okCount, int nokCount)> ObtenerResumenKpisAsync(
            int? identificadorId,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? busqueda)
        {
            var query = ConstruirFiltroParametros(identificadorId, null, fechaInicio, fechaFin, busqueda);

            var agrupados = await query
                .GroupBy(p => p.EstatusCalidad)
                .Select(g => new { Estatus = g.Key, Total = g.Count() })
                .ToListAsync();

            int ok = agrupados.FirstOrDefault(g => g.Estatus != null && g.Estatus.Equals("OK", StringComparison.OrdinalIgnoreCase))?.Total ?? 0;
            int nok = agrupados.FirstOrDefault(g => g.Estatus != null && g.Estatus.Equals("NOK", StringComparison.OrdinalIgnoreCase))?.Total ?? 0;
            int total = agrupados.Sum(g => g.Total);

            return (total, ok, nok);
        }

        public async Task<int> ContarParametrosTotalAsync(
            int? identificadorId,
            string? estatus,
            DateTime? fechaInicio,
            DateTime? fechaFin,
            string? busqueda)
        {
            var query = ConstruirFiltroParametros(identificadorId, estatus, fechaInicio, fechaFin, busqueda);
            return await query.CountAsync();
        }

        public async Task<IEnumerable<int>> ObtenerSalidasPorMaquinaAsync(int idMaquina)
        {
            return await _context.Parametros
                                 .AsNoTracking()
                                 .Where(parametro => parametro.IdentificadorId == idMaquina && parametro.Salida.HasValue)
                                 .Select(parametro => parametro.Salida!.Value)
                                 .Distinct()
                                 .OrderBy(salida => salida)
                                 .ToListAsync();
        }

        public async Task<Parametro?> AgregarParametroIngestaAsync(Parametro parametro)
        {
            parametro.FechaCreacion = DateTime.Now;
            var entityEntry = await _context.Parametros.AddAsync(parametro);
            await _context.SaveChangesAsync();
            return entityEntry.Entity;
        }

        // =============================================================
        // REVALIDACIÓN DE CALIDAD
        // =============================================================
        public async Task<(int totalProcesados, int cambiaronOk, int cambiaronNok)> RevalidarParametrosAsync(
            int? identificadorId,
            DateTime? fechaInicio,
            DateTime? fechaFin)
        {
            var query = _context.Parametros.AsQueryable();

            if (identificadorId.HasValue && identificadorId.Value > 0)
                query = query.Where(p => p.IdentificadorId == identificadorId.Value);

            if (fechaInicio.HasValue)
                query = query.Where(p => p.Fecha >= fechaInicio.Value.Date);

            if (fechaFin.HasValue)
            {
                var finLimite = fechaFin.Value.Date.AddDays(1);
                query = query.Where(p => p.Fecha < finLimite);
            }

            var parametros = await query.ToListAsync();
            if (parametros.Count == 0) return (0, 0, 0);

            var recetasActivas = await _context.Receta
                .AsNoTracking()
                .Where(r => r.Estado == true)
                .ToListAsync();

            int totalProcesados = 0;
            int cambiaronOk = 0;
            int cambiaronNok = 0;

            static string NormalizarNombre(string? texto)
            {
                if (string.IsNullOrWhiteSpace(texto)) return "";
                return texto.Trim().ToLower()
                    .Replace("á", "a")
                    .Replace("é", "e")
                    .Replace("í", "i")
                    .Replace("ó", "o")
                    .Replace("ú", "u")
                    .Replace(" ", "");
            }

            foreach (var p in parametros)
            {
                if (!p.IdentificadorId.HasValue || !p.Salida.HasValue) continue;

                var recetasSalida = recetasActivas
                    .Where(r => r.IdMaquina == p.IdentificadorId.Value && 
                                r.Salida == p.Salida.Value && 
                                (r.MinVal != 0 || r.MaxVal != 0))
                    .ToList();

                List<string> fallas = new();

                if (recetasSalida.Count == 0)
                {
                    fallas.Add($"Sin receta de control activa configurada para Salida {p.Salida.Value}");
                }
                else
                {
                    void Validar(string parametro, double? valor)
                    {
                        string paramNorm = NormalizarNombre(parametro);
                        var r = recetasSalida.FirstOrDefault(rec => NormalizarNombre(rec.Parametro) == paramNorm);

                        if (r != null && valor.HasValue)
                        {
                            double min = Math.Min(r.MinVal, r.MaxVal);
                            double max = Math.Max(r.MinVal, r.MaxVal);

                            if (valor.Value < min || valor.Value > max)
                            {
                                fallas.Add($"{parametro}: {valor.Value} [Min:{r.MinVal}, Max:{r.MaxVal}]");
                            }
                        }
                    }

                    Validar("Corriente", p.Corriente);
                    Validar("Energia", p.Energia);
                    Validar("Tiempo", p.Tiempo);
                    Validar("Penetracion", p.Penetracion);
                    Validar("VolArc", p.VolArc);
                    Validar("VolPri", p.VolPri);
                }

                string estatusPrevio = (p.EstatusCalidad ?? "").Trim().ToUpper();
                string nuevoEstatus = fallas.Count == 0 ? "OK" : "NOK";
                string? nuevosDetalles = fallas.Count == 0 ? null : string.Join(" | ", fallas);

                if (estatusPrevio != "OK" && nuevoEstatus == "OK")
                {
                    cambiaronOk++;
                }
                else if (estatusPrevio == "OK" && nuevoEstatus == "NOK")
                {
                    cambiaronNok++;
                }

                p.EstatusCalidad = nuevoEstatus;
                p.DetallesFallas = nuevosDetalles;
                totalProcesados++;
            }

            await _context.SaveChangesAsync();
            return (totalProcesados, cambiaronOk, cambiaronNok);
        }
    }
}