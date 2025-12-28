using System.Linq.Expressions;
using BaseService.Application.Common;

namespace BaseService.Application.Interfaces.Repositories;

public interface IQueryRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Find entities by predicate as asynchronous
    /// </summary>
    Task<List<TEntity>> ToListAsync();

    /// <summary>
    /// Find entities by predicate as asynchronous
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<List<TEntity>> ToListAsync(Expression<Func<TEntity, bool>> predicate);
    
    /// <summary>
    /// Get paged entities.
    /// </summary>
    Task<PagedResult<TEntity>> PagedAsync(int? pageNumber, int? pageSize, Expression<Func<TEntity, bool>> predicate);
    
    /// <summary>
    /// Get paged entities.
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    Task<PagedResult<TEntity>> PagedAsync(int? pageNumber, int? pageSize);

    /// <summary>
    /// Find entities by predicate
    /// </summary>
    /// <returns></returns>
    IEnumerable<TEntity> ToList();
    
    /// <summary>
    /// Find entity by predicate
    /// </summary>
    /// <param name="predicate"></param>
    /// <returns></returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null);

    /// <summary>
    /// Get or set a collection in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task<TEntity?> GetOrSetAsync(string key, Func<Task<TEntity?>> factory, TimeSpan? expiry = null);

    /// <summary>
    /// Get or set a list of entities in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task<List<TEntity>> GetOrSetListAsync(string key, Func<Task<List<TEntity>>> factory, TimeSpan? expiry = null);
    
    /// <summary>
    /// Get or set a paged list of entities in cache
    /// </summary>
    /// <param name="key"></param>
    /// <param name="factory"></param>
    /// <param name="expiry"></param>
    /// <returns></returns>
    Task<PagedResult<TEntity>> GetOrSetPagedAsync(string key, Func<Task<PagedResult<TEntity>>> factory, TimeSpan? expiry = null);
}