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
            options.DatabaseSchemaName = "StudentServiceDB_Marten";

            options.Schema.For<StudentCollection>().Identity(x => x.StudentId);
            options.Schema.For<LearningGoalCollection>().Identity(x => x.GoalId);
            options.Schema.For<TechnologyCollection>().Identity(x => x.TechnologyId);
            options.Schema.For<LearningPathCourseCollection>().Identity(x => x.InternalCourseId);
            options.Schema.For<LearningPathMajorCollection>().Identity(x => x.LearningPathMajorId);
            options.Schema.For<LearningPathCourseCollection>().Identity(x => x.LearningPathCourseId);
            options.Schema.For<LearningPathCollection>().Identity(x => x.PathId);
            options.Schema.For<StudentTechnologyCollection>().Identity(x => x.Id);
            options.Schema.For<StudentTechnologyCollection>().Identity(x => x.Id);
            options.Schema.For<StudentOrientationCollection>().Identity(x => x.StudentOrientationId);
            options.Schema.For<UserBehaviourCollection>().Identity(x => x.Id);
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