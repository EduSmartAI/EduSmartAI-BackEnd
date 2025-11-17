using BaseService.Application.Interfaces.Commons;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Logics;
using BaseService.Infrastructure.Repositories;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TeacherService.Application.Interfaces;
using TeacherService.Domain.ReadModels;
using TeacherService.Domain.WriteModels;
using TeacherService.Infrastructure.Contexts;

namespace TeacherService.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			var connectionString = Environment.GetEnvironmentVariable(ConstEnv.TeacherServiceDb);
			var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;

			services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
			services.AddScoped<IDatabase>(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

			// Entity Framework configuration
			services.AddDbContext<TeacherServiceContext>(options =>
			{
				options.UseNpgsql(connectionString);
			});

			services.AddScoped<AppDbContext, TeacherServiceContext>();

			// Repository services
			services.AddHttpContextAccessor();
			services.AddScoped<ICommonLogic, CommonLogic>();
			services.AddScoped<IIdentityService, IdentityService>();
			services.AddScoped<IUnitOfWork, UnitOfWork>();

			// Command repositories
			services.AddScoped<ICommandRepository<Teacher>, CommandRepository<Teacher>>();
			services.AddScoped<ICommandRepository<TeacherCertificate>, CommandRepository<TeacherCertificate>>();
			services.AddScoped<ICommandRepository<TeacherQualification>, CommandRepository<TeacherQualification>>();
			services.AddScoped<ICommandRepository<TeacherExperience>, CommandRepository<TeacherExperience>>();
			services.AddScoped<ICommandRepository<OutboxMessage>, CommandRepository<OutboxMessage>>();


			// Query repositories
			services.AddScoped<IQueryRepository<TeacherCollection>, QueryRepository<TeacherCollection>>();

			// Services
			services.AddScoped<ITeacherService, Implements.TeacherService>();

			services.AddMarten(options =>
			{
				options.Connection(connectionString!);
				options.AutoCreateSchemaObjects = AutoCreate.All;
				options.DatabaseSchemaName = "TeacherServiceDB_Marten";

				options.Schema.For<TeacherCollection>().Identity(x => x.TeacherId);
			});
			return services;
		}

		public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
		{
			using var scope = app.Services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<TeacherServiceContext>();
			await db.Database.EnsureCreatedAsync();
			return app;
		}
	}
}
