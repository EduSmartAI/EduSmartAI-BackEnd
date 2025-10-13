using BaseService.Common.Settings;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using Course.Application.Consumers;
using Course.Application.Consumers.GetInfoInternalCourse;
using MassTransit;

namespace Course.API.Extensions
{
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
                x.AddConsumer<CourseMajorSemesterSelectEventConsumer>();
                x.AddConsumer<SemesterSelectsConsumer>();
                x.AddConsumer<MajorSelectsConsumer>();
                x.AddConsumer<SubjectSelectsConsumer>();
                x.AddConsumer<CoursesSelectConsumer>();
                x.AddConsumer<GetInfoInternalCourseConsumer>();

                x.SetEndpointNameFormatter(new KebabCaseEndpointNameFormatter(prefix: "course", includeNamespace: false));

                x.UsingRabbitMq((context, cfg) =>
                {
                    cfg.Host(rabbitMqHost, "/", h =>
                    {
                        h.Username(rabbitMqUsername!);
                        h.Password(rabbitMqPassword!);
                    });

                    cfg.ConfigureEndpoints(context);
                });

                x.AddRequestClient<QuizCourseInsertEvent>();
                x.AddRequestClient<QuizCourseSelectEvent>();
            });

            return services;
        }
    }
}
