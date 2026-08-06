using System.Linq.Expressions;

namespace Enhartt.Domain.Repositories
{
    public interface IRepository<T> where T : class
    {
        // Lecturas (GET)
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

        // Creación (POST)
        Task AddAsync(T entity);

        // Actualización Completa / Parcial (PUT / PATCH)
        void Update(T entity);

        // Confirmación de transacciones
        Task SaveChangesAsync();

        // DELETE omitido intencionalmente por políticas de trazabilidad
    }
}