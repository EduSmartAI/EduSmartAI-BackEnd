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
    /// Save all changes.
    /// </summary>
    /// <param name="email"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="needLogicalDelete"></param>
    /// <returns></returns>
    Task<int> SaveChangesAsync(string email, CancellationToken cancellationToken, bool needLogicalDelete = false);
    
    /// <summary>
    /// Store a collection of entities in the marten.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    void Store<TCollection>(TCollection entity) where TCollection : class?;
    
    void Store<TCollection>(IEnumerable<TCollection> entities) where TCollection : class;
    
    /// <summary>
    /// Delete a collection of entities from the marten.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    void Delete<TCollection>(TCollection entity) where TCollection : class?;
    
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
    
    /// <summary>
    /// Set a value in Redis cache with optional expiration
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache (will be serialized to JSON)</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns></returns>
    Task CacheSetAsync<T>(string key, T value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Set a string value in Redis cache with optional expiration
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">String value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <returns></returns>
    Task CacheSetStringAsync(string key, string value, TimeSpan? expiration = null);
    
    /// <summary>
    /// Get a value from Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>Cached value or default if not found</returns>
    Task<T?> CacheGetAsync<T>(string key) where T : class;
    
    /// <summary>
    /// Get a string value from Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cached string value or null if not found</returns>
    Task<string?> CacheGetStringAsync(string key);
    
    /// <summary>
    /// Check if a key exists in Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists, false otherwise</returns>
    Task<bool> CacheExistsAsync(string key);
}