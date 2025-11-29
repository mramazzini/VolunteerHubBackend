using System.Linq.Expressions;

namespace SixSeven.Application.Interfaces.Repositories;

public interface IGenericRepository<T> where T : class
{
    /// <summary>
    /// Adds an entity to the change tracker for insertion on SaveAsync.
    /// </summary>
    void QueueInsert(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists all queued changes to the underlying store.
    /// </summary>
    Task<int> SaveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a single record matching the predicate, or null if none found.
    /// </summary>
    Task<T?> FindAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all records matching the predicate.
    /// </summary>
    Task<IReadOnlyList<T>> GetAsync(
        Expression<Func<T, bool>> predicate,
        CancellationToken cancellationToken = default);
}