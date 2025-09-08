using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using StudentService.Domain.ReadModels;
using StudentService.Infrastructure.Contexts;

namespace StudentService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.StudentServiceDb);
        var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<StudentServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, StudentServiceContext>();
        
        // Marten document database configuration
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
            options.AutoCreateSchemaObjects = AutoCreate.All;
            options.DatabaseSchemaName = "UserServiceDB_Marten";
            
            options.Schema.For<StudentCollection>().Identity(x => x.StudentId);
            options.Schema.For<TeacherCollection>().Identity(x => x.TeacherId);
            options.Schema.For<TeacherRatingCollection>().Identity(x => x.RatingId);
        });
        
        return services;
    }
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<StudentServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}