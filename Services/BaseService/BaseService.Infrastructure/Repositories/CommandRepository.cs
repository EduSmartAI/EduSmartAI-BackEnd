using BaseService.Application.Common;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

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
        int? pageNumber,
        int? pageSize,
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
        
        // Validate pageNumber, pageSize
        int validPageNumber = pageNumber.GetValueOrDefault(1);
        if (validPageNumber < 1) validPageNumber = 1;

        int validPageSize = pageSize.GetValueOrDefault(10);
        if (validPageSize <= 0) validPageSize = 10;

        // Apply sorting
        if (orderBy != null) query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

        // Count total
        var totalCount = await query.CountAsync(cancellationToken);
        
        // Apply paging
        int skip = (validPageNumber - 1) * validPageSize;
        var items = await query
            .Skip(skip)
            .Take(validPageSize)
            .ToListAsync(cancellationToken);

        // Return paged result
        return new PagedResult<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = validPageNumber,
            PageSize = validPageSize
        };

    }

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
	public async Task<PagedResult<TEntity>> PagedAsync<TKey>(
	    int? pageNumber,
	    int? pageSize,
	    Expression<Func<TEntity, bool>>? predicate = null,
	    Expression<Func<TEntity, TKey>>? orderBy = null,
	    bool orderByDescending = false,
	    CancellationToken cancellationToken = default,
	    Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null)
	{
		IQueryable<TEntity> query = DbSet.AsNoTracking();

		if (include != null) query = include(query);
		if (predicate != null) query = query.Where(predicate);
		// sort
		if (orderBy != null) query = orderByDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

		int validPageNumber = Math.Max(1, pageNumber ?? 1);
		int validPageSize = Math.Max(1, pageSize ?? 10);

		var totalCount = await query.CountAsync(cancellationToken);
		var items = await query
			.Skip((validPageNumber - 1) * validPageSize)
			.Take(validPageSize)
			.ToListAsync(cancellationToken);

		return new PagedResult<TEntity>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = validPageNumber,
			PageSize = validPageSize
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
	public IQueryable<TEntity?> Find(Expression<Func<TEntity, bool>>? predicate = null, bool isTracking = false,
        CancellationToken cancellationToken = default,
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
	/// Get IQueryable for the entity with ThenInclude support.
	/// </summary>
	/// <param name="predicate">Filter predicate</param>
	/// <param name="isTracking">Enable change tracking</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <param name="include">Include function supporting ThenInclude</param>
	/// <returns></returns>
	public IQueryable<TEntity?> Find(
		Expression<Func<TEntity, bool>>? predicate = null,
		bool isTracking = false,
		CancellationToken cancellationToken = default,
		Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null)
	{
		// Start with the DbSet
		IQueryable<TEntity> query = DbSet;

		// Apply includes first (if provided)
		if (include != null) query = include(query);

		// Apply the predicate if provided
		if (predicate != null) query = query.Where(predicate);

		// Apply tracking behavior
		if (!isTracking) query = query.AsNoTracking();

		// Return the constructed query
		return query;
	}

    /// <summary>
    /// Get the first entity matching the predicate.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="includes"></param>
    /// <returns></returns>
    public async Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken cancellationToken = default,
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
    /// <param name="userEmail"></param>
    public async Task AddAsync(TEntity entity, string userEmail)
    {
        var now = DateTime.UtcNow;
        dynamic dyn = entity;
        dyn.IsActive = true;
        dyn.CreatedAt = now;
        dyn.CreatedBy = userEmail;
        dyn.UpdatedAt = now;
        dyn.UpdatedBy = userEmail;
        await context.AddAsync(entity);
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
    /// <param name="userEmail"></param>
    public async Task AddRangeAsync(IEnumerable<TEntity> entities, string userEmail)
    {
        var now = DateTime.UtcNow;
        foreach (dynamic entity in entities)
        {
            entity.IsActive = true;
            entity.CreatedAt = now;
            entity.CreatedBy = userEmail;
            entity.UpdatedAt = now;
            entity.UpdatedBy = userEmail;
        }
        await context.AddRangeAsync(entities);
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
    /// <param name="userEmail"></param>
    /// <param name="needLogicalDelete"></param>
    public void Update(TEntity entity, string userEmail, bool needLogicalDelete = false)
    {
        var now = DateTime.UtcNow;
        dynamic dyn = entity;
        
        dyn.UpdatedAt = now;
        dyn.UpdatedBy = userEmail;
        if (needLogicalDelete)
        {
            dyn.IsActive = false;
        }
        else
        {
            dyn.IsActive = true;
        }
        DbSet.Update(entity);
    }

    /// <summary>
    /// Update entity in the database
    /// </summary>
    /// <param name="entity"></param>
    public void Update(TEntity entity)
    {
        context.Update(entity);
    }

    /// <summary>
    /// Update a range of entities in the database
    /// </summary>
    /// <param name="entities"></param>
    /// <param name="userEmail"></param>
    /// <param name="needLogicalDelete"></param>
    public void UpdateRange(IEnumerable<TEntity> entities, string userEmail, bool needLogicalDelete = false)
    {
        var now = DateTime.UtcNow;
        foreach (dynamic entity in entities)
        {
            entity.UpdatedAt = now;
            entity.UpdatedBy = userEmail;
            if (needLogicalDelete)
            {
                entity.IsActive = false;
            }
            else
            {
                entity.IsActive = true;
            }
        }
        DbSet.UpdateRange(entities);
    }

    public void UpdateRange(IEnumerable<TEntity> entities)
    {
        context.UpdateRange(entities);
    }
}

