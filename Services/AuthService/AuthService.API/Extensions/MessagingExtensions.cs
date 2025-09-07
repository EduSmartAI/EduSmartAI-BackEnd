using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;

namespace AuthService.API.Extensions;

public static class MessagingExtensions
{
    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        services.AddMassTransit(x =>
        {
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
            
            // Add RequestClient for UserLoginEvent
            x.AddRequestClient<UserLoginEvent>();
        });
        
        return services;
    }
}