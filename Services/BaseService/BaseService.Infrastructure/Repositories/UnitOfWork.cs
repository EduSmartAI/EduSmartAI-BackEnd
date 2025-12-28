using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Contexts;
using Marten;
using StackExchange.Redis;
using System.Text.Json;

namespace BaseService.Infrastructure.Repositories;

public class UnitOfWork(AppDbContext context, IDocumentSession session, IDatabase cache) : IUnitOfWork
{
 
    /// <summary>
    /// The database context for the unit of work.
    /// </summary>
    public void Dispose()
    {
        context?.Dispose();
        session?.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Begin a new transaction and execute the provided action.
    /// </summary>
    /// <param name="action"></param>
    /// <param name="cancellationToken"></param>
    public async Task BeginTransactionAsync(Func<Task<bool>> action, CancellationToken cancellationToken = default)
    {
        // Begin transaction
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Execute action
            if (await action())
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(cancellationToken);
            }
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
    
    /// <summary>
    /// Save all changes to the database
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync( string email, CancellationToken cancellationToken, bool needLogicalDelete = false)
    {
        return await context.SaveChangesAsync(email, cancellationToken, needLogicalDelete);
    }

    /// <summary>
    /// Store a collection of entities in the Marten session.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    public void Store<TCollection>(TCollection entity) where TCollection : class
    {
        session.Store(entity);
    }

    /// <summary>
    /// Store a collection of entities in the Marten session.
    /// </summary>
    /// <param name="entities"></param>
    /// <typeparam name="TCollection"></typeparam>
    public void Store<TCollection>(IEnumerable<TCollection> entities) where TCollection : class
    {
        session.Store(entities);
    }

    /// <summary>
    /// Delete a collection of entities from the Marten session.
    /// </summary>
    /// <param name="entity"></param>
    /// <typeparam name="TCollection"></typeparam>
    public void Delete<TCollection>(TCollection entity) where TCollection : class
    {
        session.Delete(entity);
    }

    /// <summary>
    /// Save all changes asynchronously
    /// </summary>
    public async Task SessionSaveChangesAsync()
    {
        await session.SaveChangesAsync();
    }
    
    /// <summary>
    /// Remove a collection from cache
    /// </summary>
    /// <param name="key"></param>
    public async Task CacheRemoveAsync(string key)
    {
        await cache.KeyDeleteAsync(key);
    }
    
    /// <summary>
    /// Set a value in Redis cache with optional expiration
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache (will be serialized to JSON)</param>
    /// <param name="expiration">Optional expiration time</param>
    /// <typeparam name="T">Type of value</typeparam>
    public async Task CacheSetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        var serializedValue = JsonSerializer.Serialize(value);
        await cache.StringSetAsync(key, serializedValue, expiration);
    }
    
    /// <summary>
    /// Set a string value in Redis cache with optional expiration
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">String value to cache</param>
    /// <param name="expiration">Optional expiration time</param>
    public async Task CacheSetStringAsync(string key, string value, TimeSpan? expiration = null)
    {
        await cache.StringSetAsync(key, value, expiration);
    }
    
    /// <summary>
    /// Get a value from Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <typeparam name="T">Type of value</typeparam>
    /// <returns>Cached value or default if not found</returns>
    public async Task<T?> CacheGetAsync<T>(string key) where T : class
    {
        var value = await cache.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
            return null;
        
        return JsonSerializer.Deserialize<T>(value!);
    }
    
    /// <summary>
    /// Get a string value from Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>Cached string value or null if not found</returns>
    public async Task<string?> CacheGetStringAsync(string key)
    {
        var value = await cache.StringGetAsync(key);
        return value.IsNullOrEmpty ? null : value.ToString();
    }
    
    /// <summary>
    /// Check if a key exists in Redis cache
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <returns>True if key exists, false otherwise</returns>
    public async Task<bool> CacheExistsAsync(string key)
    {
        return await cache.KeyExistsAsync(key);
    }
}