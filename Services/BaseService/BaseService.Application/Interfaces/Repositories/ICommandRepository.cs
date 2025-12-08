using System.Linq.Expressions;
using BaseService.Application.Common;
using Microsoft.EntityFrameworkCore.Query;

namespace BaseService.Application.Interfaces.Repositories;

public interface ICommandRepository<TEntity> where TEntity : class
{
    /// <summary>
    /// Get IQueryable for the entity.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="isTracking"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    IQueryable<TEntity?> Find(Expression<Func<TEntity, bool>>? predicate = null, bool isTracking = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Get IQueryable for the entity with ThenInclude support.
    /// </summary>
    /// <param name="predicate">Filter predicate</param>
    /// <param name="isTracking">Enable change tracking</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <param name="include">Include function supporting ThenInclude</param>
    /// <returns></returns>
    IQueryable<TEntity?> Find(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool isTracking = false,
        CancellationToken cancellationToken = default,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null);

   /// <summary>
   /// Get first entity matching the predicate.
   /// </summary>
   /// <param name="predicate"></param>
   /// <param name="cancellationToken"></param>
   /// <param name="includes"></param>
   /// <returns></returns>
    Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default, params Expression<Func<TEntity, object>>[] includes);

    /// <summary>
    /// Get paged entities.
    /// </summary>
    Task<PagedResult<TEntity>> PagedAsync<TKey>(
        int? pageNumber,
        int? pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includes);

	/// <summary>
	/// Get paged entities with include support.
	/// </summary>
	/// <typeparam name="TKey"></typeparam>
	/// <param name="pageNumber"></param>
	/// <param name="pageSize"></param>
	/// <param name="predicate"></param>
	/// <param name="orderBy"></param>
	/// <param name="orderByDescending"></param>
	/// <param name="cancellationToken"></param>
	/// <param name="include"></param>
	/// <returns></returns>
	Task<PagedResult<TEntity>> PagedAsync<TKey>(
        int? pageNumber,
        int? pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null);

	/// <summary>
	/// Add entity to the database.
	/// </summary>
	/// <param name="entity"></param>
	/// <returns></returns>
	Task AddAsync(TEntity entity, string userEmail);
    
    /// <summary>
    /// Add entity to the database.
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    Task AddAsync(TEntity entity);

    /// <summary>
    /// Add a range of entities to the database asynchronously.
    /// </summary>
    /// <param name="entities"></param>
    /// <returns></returns>
    Task AddRangeAsync(IEnumerable<TEntity> entities, string userEmail);
    
    Task AddRangeAsync(IEnumerable<TEntity> entities);


    /// <summary>
    /// Update entity in the database.
    /// </summary>
    /// <param name="entity"></param>
    void Update(TEntity entity, string userEmail, bool needLogicalDelete = false);
    
    /// <summary>
    /// Update entity in the database.
    /// </summary>
    /// <param name="entity"></param>
    void Update(TEntity entity);

    /// <summary>
    /// Update a range of entities in the database.
    /// </summary>
    /// <param name="entities"></param>
    void UpdateRange(IEnumerable<TEntity> entities, string userEmail, bool needLogicalDelete = false);
    
    void UpdateRange(IEnumerable<TEntity> entities);
}