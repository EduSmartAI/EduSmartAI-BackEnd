using System.Linq.Expressions;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Shared.Application.Common;

namespace BaseService.Infrastructure.Repositories;

public class CommandRepository<TEntity>(AppDbContext context) : ICommandRepository<TEntity> where TEntity : class
{
    private DbSet<TEntity> DbSet => context.Set<TEntity>();
    
    /// <summary>
    /// Get paged entities.
    /// </summary>
    /// <param name="pageNumber"></param>
    /// <param name="pageSize"></param>
    /// <param name="predicate"></param>
    /// <param name="orderBy"></param>
    /// <param name="orderByDescending"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <returns></returns>
    public async Task<PagedResult<TEntity>> PagedAsync<TKey>(
        int pageNumber,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        Expression<Func<TEntity, TKey>>? orderBy = null,
        bool orderByDescending = false,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[]? includes)
    {
        IQueryable<TEntity> query = DbSet;

        // Apply includes
        if (includes != null) query = includes.Aggregate(query, (current, inc) => current.Include(inc));

        // Apply filter
        if (predicate != null) query = query.Where(predicate);

        // Apply sorting
        if (orderBy != null)  query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        // Count total
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply paging
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        // Return paged result
        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// Get IQueryable for the entity.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="isTracking"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public IQueryable<TEntity?> Find(Expression<Func<TEntity, bool>>? predicate = null, bool isTracking = false, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // Start with the DbSet
        IQueryable<TEntity> query = DbSet;

        // Apply the predicate if provided
        if (predicate != null) query = query.Where(predicate);

        // Apply includes
        query = includes.Aggregate(query, (current, inc) => current.Include(inc));

        // Apply tracking behavior
        if (!isTracking) query = query.AsNoTracking();

        // Return the constructed query
        return query;
    }

    /// <summary>
    /// Get first entity matching the predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] includes)
    {
        // Start with the DbSet
        var query = DbSet.AsQueryable();
        
        // Apply the predicate if provided
        if (predicate != null) query = query.Where(predicate);

        // Apply includes
        query = includes.Aggregate(query, (current, inc) => current.Include(inc));
        
        // Execute the query and return the first or default entity
        return await query.FirstOrDefaultAsync(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Add entity to the database
    /// </summary>
    /// <param name="entity"></param>
    public async Task AddAsync(TEntity entity)
    {
        await context.AddAsync(entity);
    }

    /// <summary>
    /// Add a range of entities to the database asynchronously
    /// </summary>
    /// <param name="entities"></param>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities)
    {
        await context.AddRangeAsync(entities);
    }

    /// <summary>
    /// Update entity in the database
    /// </summary>
    /// <param name="entity"></param>
    public void Update(TEntity entity)
    {
		context.ChangeTracker.Clear();
		DbSet.Update(entity);
    }

    /// <summary>
    /// Update a range of entities in the database
    /// </summary>
    /// <param name="entities"></param>
    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        DbSet.UpdateRange(entities);
    }
}