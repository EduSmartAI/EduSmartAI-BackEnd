using AiService.Application.Consumers.AiQuizEvaluates;
using AiService.Application.Consumers.StudentInterestSurveyAnalysis;
using AiService.Application.Consumers.StudentMajorRecommends;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
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
            x.AddConsumer<QuizEvaluableCreatedEventConsumer>();

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
            x.AddRequestClient<InsertLearningPathEvent>(new Uri("queue:student-service.insert-learning-path"));
            x.AddRequestClient<UpdateExternalMajorEvent>(new Uri("queue:student-service.update-external-major"));
            
            // Add request clients
            x.AddRequestClient<UpdateBatchExternalMajorEvent>();
            x.AddRequestClient<InternalMajorEvent>(TimeSpan.FromSeconds(170));
            x.AddRequestClient<UpdateBatchExternalMajorEvent>(TimeSpan.FromSeconds(230));
        });

        return services;
    }
}