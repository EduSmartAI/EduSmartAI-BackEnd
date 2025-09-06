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
    /// <param name="userName"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="needLogicalDelete"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(string userName, CancellationToken cancellationToken = default, bool needLogicalDelete = false);
    
    /// <summary>
    /// Store a collection of entities in the marten.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    void Store<TCollection>(TCollection entity) where TCollection : class;
    
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
}