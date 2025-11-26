using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using MassTransit;
using TeacherService.Application.Applications.Teachers.Consumers;

namespace TeacherService.API.Extensions;

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
            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: "teacher", includeNamespace: false));

            // Add consumers
            x.AddConsumer<LecturerInsertEventConsumer>();
            x.AddConsumer<TeacherLoginEventConsumer>();
            x.AddConsumer<GetTeacherNameEventConsumer>();
            x.AddConsumer<GetTeacherDetailsEventConsumer>();

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