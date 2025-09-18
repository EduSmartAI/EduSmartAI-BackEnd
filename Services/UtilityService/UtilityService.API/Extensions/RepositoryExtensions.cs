using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using UtilityService.Application.Feature.UploadVideo;
using UtilityService.Application.Interfaces;
using UtilityService.Infrastructure.Implements;

namespace UtilityService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IEmailTemplateRepository, EmailTemplateRepository>();
        services.AddScoped<ICloudinaryService, CloudinaryService>();
        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));

        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssemblyContaining<VideoUploadRequest>());

        // Services


        return services;
    }
}