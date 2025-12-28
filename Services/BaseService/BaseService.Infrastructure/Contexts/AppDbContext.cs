using BaseService.Common.Utils;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace BaseService.Infrastructure.Contexts;

public abstract class AppDbContext(DbContextOptions options) : DbContext(options)
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Save changes async with common value
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public　async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Save changes async with common value
    /// </summary>
    /// <param name="updateUserId"></param>
    /// <param name="cancellationToken"></param>
    /// <param name="needLogicalDelete"></param>
    /// <returns></returns>
    public　async Task<int> SaveChangesAsync(string updateUserId, CancellationToken cancellationToken = default, bool needLogicalDelete = false)
    {
        this.SetCommonValue(updateUserId, needLogicalDelete);
        return await base.SaveChangesAsync(cancellationToken);
    }
    
    /// <summary>
    /// Set common value for all entities
    /// </summary>
    /// <param name="updateUser"></param>
    /// <param name="needLogicalDelete"></param>
    private void SetCommonValue(string updateUser, bool needLogicalDelete = false)
    {
        // Register (Add)
        var newEntities = ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Added)
            .Select(e => e.Entity);

        // Modify (update)
        var modifiedEntities = ChangeTracker.Entries()
            .Where(x => x.State == EntityState.Modified)
            .Select(e => e.Entity);

        // Get current time
        var now = DateTime.UtcNow;

        // Set newEntities
        foreach (dynamic newEntity in newEntities)
        {
            try
            {
                newEntity.IsActive = true;
                newEntity.CreatedAt = now;
                newEntity.CreatedBy = updateUser;
                newEntity.UpdatedBy = updateUser;
                newEntity.UpdatedAt = now;
            }
            catch (IOException e)
            {
                _logger.Error(e, "Error setting common values for new entity.");
            }
        }

        // Set modifiedEntities
        foreach (dynamic modifiedEntity in modifiedEntities)
        {
            try
            {
                if (needLogicalDelete)
                {
                    // Delete
                    modifiedEntity.IsActive = false;
                    modifiedEntity.UpdatedBy = updateUser;
                }
                else
                {
                    // Normal
                    modifiedEntity.IsActive = true;
                    modifiedEntity.UpdatedBy = updateUser;
                }
                modifiedEntity.UpdatedAt = now;
            }
            catch (IOException e)
            {
                _logger.Error(e, "Error setting common values for modified entity.");
            }
        }

    }
}