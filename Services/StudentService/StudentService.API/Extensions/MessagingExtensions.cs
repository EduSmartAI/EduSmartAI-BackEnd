using System.Reflection;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using StudentService.Application.Applications.ExternalConsumers;
using StudentService.Application.Applications.Students.Consumers;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
using StudentService.Application.Consumers;

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
            x.AddConsumer<StudentInformationInsertConsumer>();
            x.AddConsumer<ExternalTechnologySelectsConsumer>();
            x.AddConsumer<ExternalLearningGoalSelectsConsumer>();
            x.AddConsumer<StudentInformationUpdatedEventConsumer>();
            x.AddConsumer<InsertLearningPathEventConsumer>();
            x.AddConsumer<InsertMajorExternalCourseConsumer>();
            x.AddConsumer<StudentInformationSelectsEventConsumer>();
            x.AddConsumer<InternalMajorEventConsumer>();
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

            x.AddRequestClient<UserInsertEvent>();
            x.AddRequestClient<UserLoginEvent>();
            x.AddRequestClient<UpdateExternalMajorEvent>();
            x.AddRequestClient<CoursesSelectEvent>();
        });

        return services;
    }
}