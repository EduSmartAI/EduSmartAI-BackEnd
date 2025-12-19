using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.AIService.AiRecommend;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using BuildingBlocks.Messaging.Events.StudentService.GetOverviewAiEvaluation;
using BuildingBlocks.Messaging.Events.StudentService.GetOverviewCourse;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using StudentService.Application.Applications.ExternalConsumers;
using StudentService.Application.Applications.Students.Consumers;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
using StudentService.Application.Applications.SuggestCourses.Consumers;
using StudentService.Application.Consumers;
using StudentService.Application.Consumers.DashboardCourse;
using StudentService.Application.Consumers.AiChatLearningPath;

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
            x.AddConsumer<StudentLoginEventConsumer>();
            x.AddConsumer<StudentInformationInsertConsumer>();
            x.AddConsumer<ExternalTechnologySelectsConsumer>();
            x.AddConsumer<ExternalLearningGoalSelectsConsumer>();
            x.AddConsumer<StudentInformationUpdatedEventConsumer>();
            x.AddConsumer<InsertLearningPathEventConsumer>();
            x.AddConsumer<InsertMajorExternalCourseConsumer>();
            x.AddConsumer<InsertBatchMajorExternalCourseConsumer>();
            x.AddConsumer<StudentInformationSelectsEventConsumer>();
            x.AddConsumer<InternalMajorEventConsumer>();
            x.AddConsumer<LearningPathUpdateStatusEventConsumer>();
            x.AddConsumer<UpsertAiQuizEvaluationEventConsumer>();
            x.AddConsumer<SuggestCourseCollectionEventConsumer>();
            x.AddConsumer<SuggestCourseForStudentEventConsumer>();
            x.AddConsumer<StudentCollectionEventConsumer>();
            x.AddConsumer<GetInfoEvaluationConsumer>();
            x.AddConsumer<GetOverviewAiEvaluationConsumer>();
            x.AddConsumer<InsertAiFeedbackOverViewConsumer>();
            x.AddConsumer<GetModuleProgressEventsConsumer>();
            x.AddConsumer<UpdateModuleFeedbackEventConsumer>();
            x.AddConsumer<CourseCompletedEventConsumer>();
            x.AddConsumer<StudentTranscriptSelectEventConsumer>();
            x.AddConsumer<GetAllLearningPathConsumer>();
            x.AddConsumer<GetLearningPathInfoConsumer>();
            x.AddConsumer<AiUpdateCourseStatusToSkippedConsumer>();
            x.AddConsumer<LearningFeedbackEventConsumer>();
            x.AddConsumer<GetStudentNameEventConsumer>();

            x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: "student", includeNamespace: false));

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

            x.AddRequestClient<StudentInsertEvent>();
            x.AddRequestClient<StudentLoginEvent>();
            x.AddRequestClient<UpdateExternalMajorEvent>();
            x.AddRequestClient<UpdateBatchExternalMajorEvent>();
            x.AddRequestClient<CoursesSelectEvent>();
            x.AddRequestClient<GetInfoInternalCourseEvents>();
            x.AddRequestClient<GetModuleDashboardEvent>();
            x.AddRequestClient<GetLessonDashboardEvent>();
            x.AddRequestClient<SelectCourseInfoEvent>();
            x.AddRequestClient<GetCoursesBySubjectAndLevelEvent>();
            x.AddRequestClient<GetCourseModuleCountEvent>();
            x.AddRequestClient<AvatarUploadEvent>();
            x.AddRequestClient<SemesterIdSelectsEvent>();
            x.AddRequestClient<GetInfoEvaluationEvent>();
            x.AddRequestClient<GetAllDetailCourseEvent>();
            x.AddRequestClient<GetOverviewCourseEvents>();
            x.AddRequestClient<GetSubjectSemesterEvent>();
            x.AddRequestClient<StudentTranscriptSelectEvent>();
            x.AddRequestClient<SearchAiRecommendImproveEvents>(TimeSpan.FromSeconds(600)); // 10 minutes timeout for AI search
            x.AddRequestClient<GetCourseBasicInfoEvent>();
            x.AddRequestClient<GetSuggestedCoursesEvent>();
		});

        return services;
    }
}