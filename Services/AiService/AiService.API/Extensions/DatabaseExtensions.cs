using BaseService.Common.Utils.Const;
using JasperFx;
using Marten;
using StackExchange.Redis;

namespace AiService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.QuizServiceDb);
        var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        // services.AddDbContext<>(options =>
        // {
        //     options.UseNpgsql(connectionString);
        // });
        
        //services.AddScoped<AppDbContext, >();
        
        // Marten document database configuration
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
            options.AutoCreateSchemaObjects = AutoCreate.All;
            options.DatabaseSchemaName = "AiServiceDB_Marten";

            // TestCollection
        });
        
        return services;
    }
    
    
    // public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    // {
    //     using var scope = app.Services.CreateScope();
    //     var db = scope.ServiceProvider.GetRequiredService<>();
    //     await db.Database.EnsureCreatedAsync();
    //     return app;
    // }
}