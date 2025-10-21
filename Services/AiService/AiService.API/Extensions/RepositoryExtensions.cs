using AiService.Application.Handler;
using AiService.Application.Interfaces;
using AiService.Infrastructure.Implements;
using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;

namespace AiService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped(typeof(ICommandRepository<>), typeof(CommandRepository<>));
        services.AddScoped(typeof(IQueryRepository<>), typeof(QueryRepository<>));

        services.AddScoped<IAdvisorService, AdvisorService>();
        services.AddScoped<IVectorSearchService, VectorSearchService>();
        services.AddScoped<IMajorService, MajorService>();
        services.AddScoped<IAISearchService, AISearchService>();
        services.AddScoped<IChatBotService, ChatBotService>();

        // Services
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<AiRecommendHandler>();
            cfg.RegisterServicesFromAssemblyContaining<AiExternalRecommendHandler>();
            cfg.RegisterServicesFromAssemblyContaining<AiBatchExternalRecommendHandler>();
        });
        return services;
    }
}