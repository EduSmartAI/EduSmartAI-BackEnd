using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Repositories;
using Course.Domain.ReadModels;
using Course.Infrastructure.Data;
using Course.Infrastructure.Helpers.Courses;
using Course.Infrastructure.Implements;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Course.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services,
            IConfiguration configuration)
        {
            // Add infrastructure services here, e.g., database context, repositories, etc.
            var connectionString = Environment.GetEnvironmentVariable(ConstEnv.CourseServiceDb);

            var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;

            // C#
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
            services.AddScoped(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

            // DbContext (PostgreSQL)
            services.AddDbContext<AppDbContext, CourseDbContext>(opt =>
                opt.UseNpgsql(connectionString).EnableDetailedErrors().EnableSensitiveDataLogging());

            // Identity
            services.AddHttpContextAccessor();
            services.AddScoped<IIdentityService, IdentityService>();


            // Repositories
            services.AddScoped<ICommandRepository<CourseEntity>, CommandRepository<CourseEntity>>();
            services.AddScoped<IQueryRepository<CourseEntity>, QueryRepository<CourseEntity>>();
            services.AddScoped<ICommandRepository<CourseStudentEnrollment>, CommandRepository<CourseStudentEnrollment>>();
            services.AddScoped<IQueryRepository<CourseStudentEnrollmentCollection>, QueryRepository<CourseStudentEnrollmentCollection>>();
            services.AddScoped<ICommandRepository<Tag>, CommandRepository<Tag>>();
            services.AddScoped<ICommandRepository<Subject>, CommandRepository<Subject>>();
            services.AddScoped<ICommandRepository<ModuleQuiz>, CommandRepository<ModuleQuiz>>();
            services.AddScoped<ICommandRepository<LessonQuiz>, CommandRepository<LessonQuiz>>();
            services.AddScoped<ICommandRepository<UserLessonProgress>, CommandRepository<UserLessonProgress>>();
            services.AddScoped<ICommandRepository<UserModuleProgress>, CommandRepository<UserModuleProgress>>();
            services.AddScoped<ICommandRepository<UserCourseProgress>, CommandRepository<UserCourseProgress>>();
            services.AddScoped<ICommandRepository<Lesson>, CommandRepository<Lesson>>();
            services.AddScoped<ICommandRepository<Major>, CommandRepository<Major>>();
            services.AddScoped<ICommandRepository<Semester>, CommandRepository<Semester>>();
            services.AddScoped<ICommandRepository<Module>, CommandRepository<Module>>();
            services.AddScoped<ICommandRepository<VMajorSemesterSubjectCourses>, CommandRepository<VMajorSemesterSubjectCourses>>();
            services.AddScoped<ICommandRepository<VMajorSemesterSubjectCourses>, CommandRepository<VMajorSemesterSubjectCourses>>();
            services.AddScoped<ICommandRepository<VwCourseInfo>, CommandRepository<VwCourseInfo>>();
            services.AddScoped<ICommandRepository<VwOverviewCourseProgress>, CommandRepository<VwOverviewCourseProgress>>();
            services.AddScoped<ICommandRepository<VUserCourseProgress>, CommandRepository<VUserCourseProgress>>();
            services.AddScoped<ICommandRepository<LessonTranscript>, CommandRepository<LessonTranscript>>();
            services.AddScoped<ICommandRepository<CourseWishlist>, CommandRepository<CourseWishlist>>();
            services.AddScoped<ICommandRepository<CourseComment>, CommandRepository<CourseComment>>();
            services.AddScoped<ICommandRepository<ModuleDiscussionComment>, CommandRepository<ModuleDiscussionComment>>();
            services.AddScoped<ICommandRepository<ModuleDiscussion>, CommandRepository<ModuleDiscussion>>();
            services.AddScoped<ICommandRepository<Note>, CommandRepository<Note>>();


            // Services
            services.AddScoped<IStudentProgressService, StudentProgressService>();
            services.AddScoped<ICourseService, CourseService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<ILessonService, LessonService>();
            services.AddScoped<IMajorService, MajorService>();
            services.AddScoped<ISemesterService, SemesterService>();
            services.AddScoped<ISubjectService, SubjectService>();
            services.AddScoped<IExternalCourseService, ExternalCourseService>();
            services.AddScoped<IVwCourseInforService, VwCourseInfoService>();
            services.AddScoped<IOverviewCourseService, OverviewCourseService>();
            services.AddScoped<IVUserModuleProgressService, VUserModuleProgressService>();
            services.AddScoped<IWishlistService, WishlistService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<IModuleDiscussionCommentService, ModuleDiscussionCommentService>();
            services.AddScoped<ILessonNoteService, LessonNoteService>();

            // Helpers
            services.AddScoped<ISlugService, SlugService>();
            services.AddScoped<ICourseCache, CourseCache>();
            services.AddScoped<ICacheKeyFactory, CacheKeyFactory>();
            services.AddScoped<ICourseMapper, CourseMapper>();
            services.AddScoped<IQuizGateway, QuizGateway>();
            services.AddScoped<IQuizEventFactory, QuizEventFactory>();

            // Unit of Work
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // Marten
            services.AddMarten(options =>
            {
                options.Connection(connectionString!);
                options.AutoCreateSchemaObjects = AutoCreate.All;
                options.DatabaseSchemaName = "CourseServiceDB_Marten";

                // CourseStudentEnrollmentCollection
                options.Schema.For<CourseStudentEnrollmentCollection>()
                    .Identity(x => x.EnrollmentId);

                // UserLessonProgressCollection
                options.Schema.For<UserLessonProgressCollection>()
                    .Identity(x => x.UserLessonProgressId);

				// CourseWishlistCollection
				options.Schema.For<CourseWishlistCollection>()
					.Identity(x => x.WishlistId);
			});

            return services;
        }

        public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CourseDbContext>();
            await db.Database.EnsureCreatedAsync();
            return app;
        }
    }
}