using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.ReadModels;
using QuizService.Infrastructure.Contexts;
using StackExchange.Redis;

namespace QuizService.API.Extensions;

public static class DatabaseExtensions
{
    public static IServiceCollection AddDatabaseServices(this IServiceCollection services)
    {
        var connectionString = Environment.GetEnvironmentVariable(ConstEnv.QuizServiceDb);
        var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;
        
        services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));        
        services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
        
        // Entity Framework configuration
        services.AddDbContext<QuizServiceContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });
        
        services.AddScoped<AppDbContext, QuizServiceContext>();
        
        // Marten document database configuration
        services.AddMarten(options =>
        {
            options.Connection(connectionString!);
            options.AutoCreateSchemaObjects = AutoCreate.All;
            options.DatabaseSchemaName = "QuizServiceDB_Marten";

            // TestCollection
            options.Schema.For<TestCollection>()
                .Identity(x => x.TestId);
            
            // QuizCollection
            options.Schema.For<QuizCollection>()
                .Identity(x => x.QuizId)
                .Duplicate(x => x.Title);

            // QuestionCollection
            options.Schema.For<QuestionCollection>()
                .Identity(x => x.QuestionId)
                .Duplicate(x => x.QuestionText);

            // AnswerCollection
            options.Schema.For<AnswerCollection>()
                .Identity(x => x.AnswerId)
                .Duplicate(x => x.AnswerText);

            // StudentTestCollection
            options.Schema.For<StudentTestCollection>()
                .Identity(x => x.StudentTestId)
                .Duplicate(x => x.StudentId);

            // StudentAnswerCollection
            options.Schema.For<StudentAnswerCollection>()
                .Identity(x => x.StudentAnswerId)
                .Duplicate(x => x.QuestionId)
                .Duplicate(x => x.AnswerId);
        });
        
        return services;
    }
    
    
    public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuizServiceContext>();
        await db.Database.EnsureCreatedAsync();
        return app;
    }
}