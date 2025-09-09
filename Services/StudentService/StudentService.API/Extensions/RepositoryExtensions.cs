using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queris;
using StudentService.Application.Applications.Majors.Commands;
using StudentService.Application.Applications.Students.Commands.Inserts;
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
        services.AddScoped<ICommandRepository<Major>, CommandRepository<Major>>();
        services.AddScoped<ICommandRepository<LearningGoal>, CommandRepository<LearningGoal>>();
        services.AddScoped<ICommandRepository<Semester>, CommandRepository<Semester>>();
        
        services.AddScoped<IQueryRepository<StudentCollection>, QueryRepository<StudentCollection>>();
        services.AddScoped<IQueryRepository<MajorCollection>, QueryRepository<MajorCollection>>();
        services.AddScoped<IQueryRepository<LearningGoalCollection>, QueryRepository<LearningGoalCollection>>();
        services.AddScoped<IQueryRepository<SemesterCollection>, QueryRepository<SemesterCollection>>();
        
        // Services
        services.AddScoped<IStudentService, Infrastructure.Implements.StudentService>();
        services.AddScoped<IMajorService, MajorService>();
        services.AddScoped<ILearningGoalService, LearningGoalService>();
        
        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<StudentInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<MajorInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<LearningGoalInsertCommandHandler>();
            cfg.RegisterServicesFromAssemblyContaining<LearningGoalsSelectQueryHandler>();
        });        
        return services;
    }
}