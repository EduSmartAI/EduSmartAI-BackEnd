using System.Linq.Expressions;
using System.Text.Json;
using BaseService.Application.Common;
using BaseService.Application.Interfaces.Repositories;
using Marten;
using StackExchange.Redis;

namespace BaseService.Infrastructure.Repositories;

public class QueryRepository<TCollection>(IDocumentSession documentSession, IDatabase cache) : IQueryRepository<TCollection> where TCollection : class
{
    /// <summary>
    /// Find entities as asynchronous
    /// </summary>
    /// <returns></returns>
    public async Task<List<TCollection>> ToListAsync()
    {
        var result = await documentSession.Query<TCollection>().ToListAsync();
        return result.ToList();
    }   
    
    /// <summary>
    /// Find entities as asynchronous
    /// </summary>
    /// <returns></returns>
    public async Task<List<TCollection>> ToListAsync(Expression<Func<TCollection, bool>> predicate)
    {
        var result = await documentSession.Query<TCollection>().Where(predicate).ToListAsync();
        return result.ToList();
    }

    public async Task<PagedResult<TCollection>> PagedAsync(int? pageNumber, int? pageSize, Expression<Func<TCollection, bool>> predicate)
    {
        var query =  documentSession.Query<TCollection>().Where(predicate);
        
        // Validate pageNumber, pageSize
        int validPageNumber = pageNumber.GetValueOrDefault(1);
        if (validPageNumber < 1) validPageNumber = 1;

        int validPageSize = pageSize.GetValueOrDefault(10);
        if (validPageSize <= 0) validPageSize = 10;
        
        // Count total
        var totalCount = await query.CountAsync();
        
        // Apply paging
        int skip = (validPageNumber - 1) * validPageSize;
        var items = await query
            .Skip(skip)
            .Take(validPageSize)
            .ToListAsync();

        // Return paged result
        return new PagedResult<TCollection>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = validPageNumber,
            PageSize = validPageSize
        };
    }
    
    public async Task<PagedResult<TCollection>> PagedAsync(int? pageNumber, int? pageSize)
    {
        var query =  documentSession.Query<TCollection>();
        
        // Validate pageNumber, pageSize
        int validPageNumber = pageNumber.GetValueOrDefault(1);
        if (validPageNumber < 1) validPageNumber = 1;

        int validPageSize = pageSize.GetValueOrDefault(10);
        if (validPageSize <= 0) validPageSize = 10;
        
        // Count total
        var totalCount = await query.CountAsync();
        
        // Apply paging
        int skip = (validPageNumber - 1) * validPageSize;
        var items = await query
            .Skip(skip)
            .Take(validPageSize)
            .ToListAsync();

        // Return paged result
        return new PagedResult<TCollection>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = validPageNumber,
            PageSize = validPageSize
        };

    }

    /// <summary>
    /// Find all entities
    /// </summary>
    /// <returns></returns>
    public IEnumerable<TCollection> ToList()
    {
        return documentSession.Query<TCollection>().ToList();
    }

    /// <summary>
    /// Find entity by predicate
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    public async Task<TCollection?> FirstOrDefaultAsync(Expression<Func<TCollection, bool>>? predicate)
    {
        if (predicate != null) return await documentSession.Query<TCollection>().FirstOrDefaultAsync(predicate);
        return await documentSession.Query<TCollection>().FirstOrDefaultAsync();
    }
    
    /// <summary>
    /// Get or set a collection in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public async Task<TCollection?> GetOrSetAsync(string key, Func<Task<TCollection?>> factory, TimeSpan? expiry = null)
    {
        var cached = await cache.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<TCollection>(cached!);

        var result = await factory();
        if (result != null)
        {
            await cache.StringSetAsync(key, JsonSerializer.Serialize(result), expiry);
        }
        return result;
    }
    
    /// <summary>
    /// Get or set a list of collections in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public async Task<List<TCollection>> GetOrSetListAsync(string key, Func<Task<List<TCollection>>> factory, TimeSpan? expiry = null)
    {
        var cached = await cache.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<List<TCollection>>(cached!) ?? new List<TCollection>();

        var result = await factory();
        if (result.Count > 0)
        {
            await cache.StringSetAsync(key, JsonSerializer.Serialize(result), expiry);
        }
        return result;
    }

    /// <summary>
    /// Get or set a paged collection in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    public async Task<PagedResult<TCollection>> GetOrSetPagedAsync(string key, Func<Task<PagedResult<TCollection>>> factory, TimeSpan? expiry = null)
    {
        var cached = await cache.StringGetAsync(key);
        if (cached.HasValue)
            return JsonSerializer.Deserialize<PagedResult<TCollection>>(cached!) ?? new PagedResult<TCollection>();
        var result = await factory();
        if (result.Items.Any())
        {
            await cache.StringSetAsync(key, JsonSerializer.Serialize(result), expiry);
        }
        return result;
    }
}