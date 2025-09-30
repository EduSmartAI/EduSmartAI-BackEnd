using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.UserLoginEvents;
using MassTransit;
using QuizService.Application.Applications.Consumers;
using QuizService.Application.Applications.QuizCourses.Consumers;
using QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;

namespace QuizService.API.Extensions;

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
            x.AddConsumer<StudentQuizCollectionInsertConsumer>();
            x.AddConsumer<QuizCourseCollectionInsertConsumer>();
            x.AddConsumer<QuizCourseSelectConsumer>();
			x.AddConsumer<QuizCourseInsertConsumer>();
			x.AddConsumer<StudentStudyTimeConsumer>();
            
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
        });
        
        return services;
    }
}