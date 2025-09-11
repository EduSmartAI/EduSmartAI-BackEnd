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
}