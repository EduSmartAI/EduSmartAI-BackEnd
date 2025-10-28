using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using TeacherService.Application.Interfaces;
using TeacherService.Domain.ReadModels;
using TeacherService.Domain.WriteModels;

namespace TeacherService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        // Command repositories
        services.AddScoped<ICommandRepository<Teacher>, CommandRepository<Teacher>>();
        services.AddScoped<ICommandRepository<OutboxMessage>, CommandRepository<OutboxMessage>>();
        
        // Query repositories
        services.AddScoped<IQueryRepository<TeacherCollection>, QueryRepository<TeacherCollection>>();
        
        // Services
        services.AddScoped<ITeacherService, Infrastructure.Implements.TeacherService>();

        // MediatR configuration
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.Applications.Teachers.Commands.Inserts.LecturerInsertCommand).Assembly));
            
        return services;
    }
}