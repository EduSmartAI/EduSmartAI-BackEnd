using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using BuildingBlocks.Messaging.Events.PaymentService;
using MassTransit;

namespace PaymentService.API.Extensions;

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
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: "payment", includeNamespace: false));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername!);
                    h.Password(rabbitMqPassword!);
                });
                
                cfg.ConfigureEndpoints(context);
            });

			x.AddRequestClient<CheckIsCourseEnrolledEvent>();
            x.AddRequestClient<QuizCourseInsertEvent>();

		});
        
        return services;
    }
}