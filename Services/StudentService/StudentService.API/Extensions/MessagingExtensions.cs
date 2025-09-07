using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using StudentService.Application.Students.Consumers;
using StudentService.Application.Users.Consumers;

namespace StudentService.API.Extensions;

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
            x.AddConsumer<UserInsertEventConsumer>();
            x.AddConsumer<UserLoginEventConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername!);
                    h.Password(rabbitMqPassword!);
                });
                
                cfg.ConfigureEndpoints(context);
            });

            x.AddRequestClient<UserInsertEvent>();
            x.AddRequestClient<UserLoginEvent>();
        });
        
        return services;
    }
}