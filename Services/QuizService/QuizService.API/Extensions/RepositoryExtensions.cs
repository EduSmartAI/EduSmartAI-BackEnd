using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using QuizService.Application.Applications.StudentTests.Commands;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Interfaces;
using QuizService.Application.Judge0Logics;
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
        services.AddScoped<ICommandRepository<StudentTest>, CommandRepository<StudentTest>>();
        services.AddScoped<ICommandRepository<StudentQuiz>, CommandRepository<StudentQuiz>>();
        services.AddScoped<ICommandRepository<StudentQuizAnswer>, CommandRepository<StudentQuizAnswer>>();
        services.AddScoped<ICommandRepository<SurveyType>, CommandRepository<SurveyType>>();
        services.AddScoped<ICommandRepository<OutboxMessage>, CommandRepository<OutboxMessage>>();
        services.AddScoped<ICommandRepository<Problem>, CommandRepository<Problem>>();
        services.AddScoped<ICommandRepository<CodeLanguage>, CommandRepository<CodeLanguage>>();
        services.AddScoped<ICommandRepository<Submission>, CommandRepository<Submission>>();
        services.AddScoped<ICommandRepository<ProblemTemplate>, CommandRepository<ProblemTemplate>>();
        services.AddScoped<ICommandRepository<ProblemExample>, CommandRepository<ProblemExample>>();
        services.AddScoped<ICommandRepository<TestCase>, CommandRepository<TestCase>>();
        services.AddScoped<ICommandRepository<Judge0Key>, CommandRepository<Judge0Key>>();
        
        services.AddScoped<IQueryRepository<TestCollection>, QueryRepository<TestCollection>>();
        services.AddScoped<IQueryRepository<QuizCollection>, QueryRepository<QuizCollection>>();
        services.AddScoped<IQueryRepository<QuestionCollection>, QueryRepository<QuestionCollection>>();
        services.AddScoped<IQueryRepository<AnswerCollection>, QueryRepository<AnswerCollection>>();
        services.AddScoped<IQueryRepository<StudentTestCollection>, QueryRepository<StudentTestCollection>>();
        services.AddScoped<IQueryRepository<StudentQuizAnswerCollection>, QueryRepository<StudentQuizAnswerCollection>>();
        services.AddScoped<IQueryRepository<StudentQuizCollection>, QueryRepository<StudentQuizCollection>>();
        
        // Services
        services.AddScoped<ITestService, TestService>();
        services.AddScoped<IQuizService, Infrastructure.Implements.QuizService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IAnswerService, AnswerService>();
        services.AddScoped<IStudentTestService, StudentTestService>();
        services.AddScoped<IStudentQuizService, StudentQuizService>();
        services.AddScoped<IQuizCourseService, QuizCourseService>();
        services.AddScoped<IQuizSurveyService, QuizSurveyService>();
        services.AddScoped<IStudentSurveyService, StudentSurveyService>();
        services.AddScoped<IPracticeTestService, PracticeTestService>();
        services.AddScoped<ILearningPathService, LearningPathService>();
        services.AddScoped<IJudge0ApiLogic, Judge0ApiLogic>();
        services.AddScoped<StudentTestServiceDependencies>();
        
        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<TestInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<StudentTestInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<StudentTestSelectQueryHandler>();
        });        
        return services;
    }
}