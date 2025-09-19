using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Repositories;
using Course.Application.Interfaces;
using Course.Domain.Models;
using Course.Infrastructure.Data;
using Course.Infrastructure.Data.Repositories;
using Course.Infrastructure.Implements;
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
			services.AddScoped<ICommandRepository<Tag>, CommandRepository<Tag>>();
			services.AddScoped<ICourseService, CourseService>();

			// Module services
			services.AddScoped<ICommandRepository<Module>, CommandRepository<Module>>();
			//services.AddScoped<IModuleRepository, ModuleRepository>();
			services.AddScoped<IModuleService, ModuleService>();

			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddMarten(options => { options.Connection(connectionString!); });

			return services;
		}
	}
}