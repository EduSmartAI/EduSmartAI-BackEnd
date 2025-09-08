using AuthService.Domain.WriteModels;
using AuthService.Infrastructure.Context;
using BaseService.Common.Utils.Const;
using Microsoft.EntityFrameworkCore;

namespace AuthService.API.Helpers;

public class RoleSeederHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public RoleSeederHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    /// <summary>
    /// Starts the hosted service and seeds the required roles into the database.
    /// This method checks if the roles "Admin", "Student", and "Lecturer"
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthServiceContext>();
        var requiredRoles = new[] { ConstRole.Admin, ConstRole.Student, ConstRole.Lecturer };

        foreach (var roleName in requiredRoles)
        {
            var exists = await context.Roles.Where(r => r.Name == roleName).ToListAsync(cancellationToken: cancellationToken);
            if (!exists.Any())
            {
                await context.AddAsync(new Role { Name = roleName , NormalizedName = roleName.ToUpper()}, cancellationToken);
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}