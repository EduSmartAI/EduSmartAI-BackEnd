using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using QuizService.Infrastructure.Implements;

namespace QuizService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICommandRepository<Test>, CommandRepository<Test>>();
        services.AddScoped<ICommandRepository<Quiz>, CommandRepository<Quiz>>();
        services.AddScoped<ICommandRepository<Question>, CommandRepository<Question>>();
        services.AddScoped<ICommandRepository<Answer>, CommandRepository<Answer>>();
        
        services.AddScoped<IQueryRepository<TestCollection>, QueryRepository<TestCollection>>();
        services.AddScoped<IQueryRepository<QuizCollection>, QueryRepository<QuizCollection>>();
        services.AddScoped<IQueryRepository<QuestionCollection>, QueryRepository<QuestionCollection>>();
        services.AddScoped<IQueryRepository<AnswerCollection>, QueryRepository<AnswerCollection>>();
        
        // Services
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<IQuizService, Infrastructure.Implements.QuizService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAnswerService, AnswerService>();
        
        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<TestInsertCommandHandler>();
        });        
        return services;
    }
}