using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using TeacherService.Domain.ReadModels;
using TeacherService.Infrastructure.Contexts;

namespace TeacherService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.TeacherServiceDb);
        var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<TeacherServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, TeacherServiceContext>();
        
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
            options.AutoCreateSchemaObjects = AutoCreate.All;
            options.DatabaseSchemaName = "TeacherServiceDB_Marten";

            options.Schema.For<TeacherCollection>().Identity(x => x.TeacherId);
        });
        return services;
    }
    
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TeacherServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}