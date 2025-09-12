using BaseService.Common.Utils.Const;
using StackExchange.Redis;

namespace UtilityService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.NotificationServiceDb);
        var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        // services.AddDbContext<Service_Context>(options =>
        // {
        //     options.UseNpgsql(connectionString);
        // });
        //
        // services.AddScoped<AppDbContext, Service_Context>();
        return services;
    }
    
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        // using var scope = app.Services.CreateScope();
        // var db = scope.ServiceProvider.GetRequiredService<Service_Context>();
        // await db.Database.EnsureCreatedAsync();
        return app;
    }
}