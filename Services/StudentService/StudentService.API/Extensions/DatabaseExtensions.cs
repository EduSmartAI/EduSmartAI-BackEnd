using BaseService.Common.Settings;
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
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.UserServiceDb);
        
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            ConnectionMultiplexer.Connect(Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!));        
        services.AddScoped<IDatabase>(sp =>
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<UserServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, UserServiceContext>();
        
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
        var db = scope.ServiceProvider.GetRequiredService<UserServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}