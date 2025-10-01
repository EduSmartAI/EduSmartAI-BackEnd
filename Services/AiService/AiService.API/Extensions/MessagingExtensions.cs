using AiService.Application.Consumers.StudentInterestSurveyAnalysis;
using AiService.Application.Consumers.StudentMajorRecommends;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using MassTransit;

namespace AiService.API.Extensions;

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
            x.AddConsumer<StudentMajorRecommendConsumer>();
            x.AddConsumer<StudentInterestSurveyAnalysisConsumer>();
            // x.AddConsumer<ExternalMajorCourseConsumer>();
            
            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername!);
                    h.Password(rabbitMqPassword!);
                });

                cfg.ConfigureEndpoints(context);
                
                // Add timeout and retry configuration
                cfg.UseMessageRetry(r => r.Exponential(5,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(5)));

                cfg.UseInMemoryOutbox();
            });
        });

        return services;
    }
}