using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queris;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Technologies.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;
using StudentService.Infrastructure.Implements;

namespace StudentService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ICommandRepository<Student>, CommandRepository<Student>>();
        services.AddScoped<ICommandRepository<LearningGoal>, CommandRepository<LearningGoal>>();
        services.AddScoped<ICommandRepository<StudentTechnology>, CommandRepository<StudentTechnology>>();
        services.AddScoped<ICommandRepository<StudentLearningGoal>, CommandRepository<StudentLearningGoal>>();
        services.AddScoped<ICommandRepository<Technology>, CommandRepository<Technology>>();
        
        services.AddScoped<IQueryRepository<StudentCollection>, QueryRepository<StudentCollection>>();
        services.AddScoped<IQueryRepository<LearningGoalCollection>, QueryRepository<LearningGoalCollection>>();
        services.AddScoped<IQueryRepository<StudentLearningGoalCollection>, QueryRepository<StudentLearningGoalCollection>>();
        services.AddScoped<IQueryRepository<TechnologyCollection>, QueryRepository<TechnologyCollection>>();
        
        // Services
        services.AddScoped<IStudentService, Infrastructure.Implements.StudentService>();
        services.AddScoped<ILearningGoalService, LearningGoalService>();
        services.AddScoped<ITechnologyService, TechnologyService>();
        
        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<StudentInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<LearningGoalInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<LearningGoalsSelectQueryHandler>();
            cfg.RegisterServicesFromAssemblyContaining<TechnologyInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<StudentMajorSemesterInsertCommandHandler>();
        });        
        return services;
    }
}