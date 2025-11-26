using AiService.Application.Consumers.AiQuizEvaluates;
using AiService.Application.Consumers.AiSearch;
using AiService.Application.Consumers.AiSummaryAndFeedback;
using AiService.Application.Consumers.CourseService;
using AiService.Application.Consumers.StudentInterestSurveyAnalysis;
using AiService.Application.Consumers.StudentMajorRecommends;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiFeedback;
using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.AIService.ModuleProgress;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using BuildingBlocks.Messaging.Events.UtilityService;
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
            x.AddConsumer<TranscribeBatchRequestedConsumer>();
            x.AddConsumer<QuizAiFeedBackOverviewEventConsumer>();
            x.AddConsumer<QuizAiFeedBackModuleEventConsumer>();
            x.AddConsumer<SearchAiRecommendImproveConsumer>();

            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: "ai", includeNamespace: false));

            x.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitMqHost, "/", h =>
                {
                    h.Username(rabbitMqUsername!);
                    h.Password(rabbitMqPassword!);
                });

                cfg.ConfigureEndpoints(context);

                cfg.UseMessageRetry(r => r.Exponential(5,
                    TimeSpan.FromSeconds(1),
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(5)));

                cfg.UseInMemoryOutbox();
            });

            x.AddRequestClient<InsertLearningPathEvent>(TimeSpan.FromSeconds(60));
            x.AddRequestClient<UpdateExternalMajorEvent>(TimeSpan.FromSeconds(60));
            x.AddRequestClient<UpdateBatchExternalMajorEvent>(TimeSpan.FromSeconds(230));
            x.AddRequestClient<GetLessonInfoEvent>(TimeSpan.FromSeconds(230));
            x.AddRequestClient<InternalMajorEvent>(TimeSpan.FromSeconds(170));
            x.AddRequestClient<InsertAiFeedbackEvents>();
            x.AddRequestClient<GetModuleProgressEvents>();
            x.AddRequestClient<GetSystemConfigEvent>(TimeSpan.FromSeconds(200));
        });

        return services;
    }
}