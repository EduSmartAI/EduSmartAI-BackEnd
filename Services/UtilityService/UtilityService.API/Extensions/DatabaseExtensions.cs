using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using Marten;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UtilityService.Infrastructure.Contexts;

namespace UtilityService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.UtilityServiceDb);
        var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<UtilityServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, UtilityServiceContext>();
        
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
        });
        return services;
    }
    
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UtilityServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}