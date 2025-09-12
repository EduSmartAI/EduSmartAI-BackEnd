namespace BaseService.Application.Interfaces.Repositories;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    /// Begin a new transaction.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task BeginTransactionAsync(Func<Task<bool>> action, CancellationToken cancellationToken = default);

    /// <summary>
    /// Save all changes.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Store a collection of entities in the marten.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    void Store<TCollection>(TCollection entity) where TCollection : class;
    
    void StoreRange<TCollection>(IEnumerable<TCollection> entities) where TCollection : class;
    
    /// <summary>
    /// Delete a collection of entities from the marten.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    void Delete<TCollection>(TCollection entity) where TCollection : class;
    
    /// <summary>
    /// Save all changes asynchronously without a user context
    /// </summary>
    /// <returns></returns>
    Task SessionSaveChangesAsync();
    
    /// <summary>
    /// Remove an entity from cache by key
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    Task CacheRemoveAsync(string key);
}