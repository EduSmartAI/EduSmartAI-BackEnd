using AuthService.Infrastructure.Context;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using OpenIddict.Abstractions;

namespace AuthService.API;

/// <summary>
/// Perform the necessary operations when the application starts
/// </summary>
public class Worker : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider"></param>
    public Worker(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Implement method
    /// </summary>
    /// <param name="cancellationToken"></param>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AuthServiceContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
        await CreateApplicationsAsync();
        async Task CreateApplicationsAsync()
        {
            // Load environment variables from .env file
            EnvLoader.Load();
            if (scope != null)
            {
                var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

                if (await manager.FindByClientIdAsync(ConstToken.Audience, cancellationToken) == null)
                {
                    var descriptor = new OpenIddictApplicationDescriptor
                    {
                        ClientId = ConstToken.Audience,
                        ClientSecret = Environment.GetEnvironmentVariable(ConstEnv.ClientSecret),
                        DisplayName = "Service client application",
                        Permissions =
                        {
                            OpenIddictConstants.Permissions.Endpoints.Introspection,
                            OpenIddictConstants.Permissions.Endpoints.Token,
                            OpenIddictConstants.Permissions.Endpoints.EndSession,
                            OpenIddictConstants.Permissions.GrantTypes.ClientCredentials,
                            OpenIddictConstants.Permissions.GrantTypes.Password,
                            OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                            OpenIddictConstants.Permissions.Prefixes.Scope,
                        },
                    };
                    await manager.CreateAsync(descriptor, cancellationToken);
                }
            }
        }
    }
    
    /// <summary>
    ///  Implement method
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

}
