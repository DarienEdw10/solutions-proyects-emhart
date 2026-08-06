using Microsoft.EntityFrameworkCore;
using Enhartt.Domain.Data;
using System.Linq.Expressions;

namespace Enhartt.Domain.Repositories
{
    public class Repository<T> : IRepository<T> where T : class
    {
        protected readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        // Consultas optimizadas para lectura (GET)
        public async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().AsNoTracking().Where(predicate).ToListAsync();
        }

        // Inserción de nuevos registros (POST)
        public async Task AddAsync(T entity)
        {
            await _context.Set<T>().AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        // Actualización Completa / Parcial (PUT / PATCH)
        public void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        // Confirmación de cambios (para PUT, PATCH o operaciones por lote)
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}