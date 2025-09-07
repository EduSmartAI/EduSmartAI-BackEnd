using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using StudentService.Application.Interfaces;
using StudentService.Application.Students.Commands.Inserts;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IQueryRepository<StudentCollection>, QueryRepository<StudentCollection>>();
        services.AddScoped<IQueryRepository<TeacherCollection>, QueryRepository<TeacherCollection>>();
        services.AddScoped<ICommandRepository<Student>, CommandRepository<Student>>();
        services.AddScoped<ICommandRepository<Teacher>, CommandRepository<Teacher>>();
        
        // Services
        services.AddScoped<IStudentService, Infrastructure.Implements.StudentService>();
        
        // MediatR configuration
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<UserInsertCommandHandler>();
        });        
        return services;
    }
}