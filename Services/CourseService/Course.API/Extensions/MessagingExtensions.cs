using BaseService.Common.Settings;
using BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents;
using BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using BuildingBlocks.Messaging.Events.StudentService.GetStudentInformation;
using BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation;
using Course.Application.Comments;
using Course.Application.Consumers;
using Course.Application.Consumers.Dashboard;
using Course.Application.Consumers.GetInfoCourse;
using Course.Application.Consumers.GetInfoInternalCourse;
using Course.Application.Consumers.GetOverviewCourse;
using Course.Application.Courses.Consumers;
using Course.Application.Dashboards.Consumers;
using Course.Application.UserLessonProgresses.Consumers;
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
                x.AddConsumer<GetModuleDashboardEventConsumer>();
                x.AddConsumer<SuggestCourseRetakeEventConsumer>();
                x.AddConsumer<GetCourseModuleCountEventConsumer>();
                x.AddConsumer<GetLessonDashboardEventConsumer>();
                x.AddConsumer<GetLessonInfoConsumer>();
                x.AddConsumer<MajorAndSemesterSelectEventConsumer>();
                x.AddConsumer<SemesterIdSelectsEventConsumer>();
                x.AddConsumer<GetInfoCourseByCourseIdConsumer>();
                x.AddConsumer<GetOverviewCourseConsumer>();
                x.AddConsumer<GetUserCourseProgressEventConsumer>();
                x.AddConsumer<GetSubjectInfoConsumer>();
                x.AddConsumer<TranscriptUpsertConsumer>();
                x.AddConsumer<CourseSelectsBySubjectCodeEventConsumer>();
                x.AddConsumer<CoreSubjectSelectEventConsumer>();
                x.AddConsumer<SelectCourseInfoEventConsumer>();
                x.AddConsumer<CheckIsCourseEnrolledEventConsumer>();
                x.AddConsumer<PaymentSucceededEventConsumer>();
                x.AddConsumer<SubjectCodeSelectEventConsumer>();
                x.AddConsumer<MappingSubjectCodeWithMajorCodeEventConsumer>();

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
                x.AddRequestClient<QuizCourseCheckAttemptEvent>();
                x.AddRequestClient<GetLatestModuleQuizScoresEvent>();
                x.AddRequestClient<GetLatestLessonQuizScoresEvent>();
				x.AddRequestClient<SuggestCourseRetakeEvent>();
				x.AddRequestClient<CourseSelectsBySubjectCodeEvent>();
				x.AddRequestClient<GetTeacherNamesEvent>();
				x.AddRequestClient<GetStudentNamesEvent>();
            });

            return services;
        }
    }
}
