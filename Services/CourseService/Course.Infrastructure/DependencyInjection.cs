using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Repositories;
using Course.Application.Interfaces;
using Course.Domain.Models;
using Course.Domain.ReadModels;
using Course.Infrastructure.Data;
using Course.Infrastructure.Implements;
using JasperFx;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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
			services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

			// DbContext (PostgreSQL)
			services.AddDbContext<AppDbContext, CourseDbContext>(opt =>
				opt.UseNpgsql(connectionString).EnableDetailedErrors().EnableSensitiveDataLogging());


			services.AddHttpContextAccessor();
			services.AddScoped<IIdentityService, IdentityService>();
			services.AddScoped<ICommandRepository<CourseEntity>, CommandRepository<CourseEntity>>();
			services.AddScoped<IQueryRepository<CourseEntity>, QueryRepository<CourseEntity>>();
			services.AddScoped<ICommandRepository<CourseStudentEnrollment>, CommandRepository<CourseStudentEnrollment>>();
			services.AddScoped<IQueryRepository<CourseStudentEnrollmentCollection>, QueryRepository<CourseStudentEnrollmentCollection>>();
			services.AddScoped<ICommandRepository<Tag>, CommandRepository<Tag>>();
			services.AddScoped<ICommandRepository<Subject>, CommandRepository<Subject>>();
			services.AddScoped<ICourseService, CourseService>();
			services.AddScoped<ICommandRepository<ModuleQuiz>, CommandRepository<ModuleQuiz>>();
			services.AddScoped<ICommandRepository<LessonQuiz>, CommandRepository<LessonQuiz>>();


			// Module services
			services.AddScoped<ICommandRepository<Module>, CommandRepository<Module>>();
			services.AddScoped<IModuleService, ModuleService>();
			services.AddScoped<ISubjectService, SubjectService>();

			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddMarten(options =>
			{
				options.Connection(connectionString!);
				options.AutoCreateSchemaObjects = AutoCreate.All;
				options.DatabaseSchemaName = "CourseServiceDB_Marten";

				// CourseStudentEnrollmentCollection
				options.Schema.For<CourseStudentEnrollmentCollection>()
					.Identity(x => x.EnrollmentId);
			});

			return services;
		}
	}
}