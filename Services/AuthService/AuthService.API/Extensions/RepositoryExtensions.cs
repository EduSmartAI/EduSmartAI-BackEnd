using AuthService.Application.Accounts.Commands.Inserts;
using AuthService.Application.Interfaces;
using AuthService.Application.Interfaces.TokenServices;
using AuthService.Domain.ReadModels;
using AuthService.Domain.WriteModels;
using AuthService.Infrastructure.Implements;
using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;

namespace AuthService.API.Extensions;

public static class RepositoryExtensions
{
    public static IServiceCollection AddRepositoryServices(this IServiceCollection services)
    {
        // Repository services
        services.AddScoped<ICommonLogic, CommonLogic>();
        services.AddScoped<ICommandRepository<Account>, CommandRepository<Account>>();
        services.AddScoped<ICommandRepository<Role>, CommandRepository<Role>>();
        services.AddScoped<IQueryRepository<AccountCollection>, QueryRepository<AccountCollection>>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAccountService, AccountService>();
        
        // MediatR configuration
        services.AddMediatR(cfg => 
            cfg.RegisterServicesFromAssemblyContaining<StudentInsertCommandHandler>());
        
        return services;
    }
}