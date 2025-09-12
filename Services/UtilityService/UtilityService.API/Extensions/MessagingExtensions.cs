using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using MassTransit;
using UtilityService.Application.Consumers.VerifyAccounts;

namespace UtilityService.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        EnvLoader.Load();
        
        var rabbitMqHost = Environment.GetEnvironmentVariable(ConstEnv.RabbitMqHost);
        var rabbitMqUsername = Environment.GetEnvironmentVariable(ConstEnv.RabbitMqUsername);
        var rabbitMqPassword = Environment.GetEnvironmentVariable(ConstEnv.RabbitMqPassword);
        
        services.AddMassTransit(x =>
        {
            x.AddConsumer<SendKeyEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername!);
                    h.Password(rabbitMqPassword!);
                });
                
                cfg.ConfigureEndpoints(context);
            });
        });
        
        return services;
    }
}