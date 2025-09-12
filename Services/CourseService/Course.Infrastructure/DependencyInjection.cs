using BaseService.Application.Interfaces.Repositories;
using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Repositories;
using Course.Application.Interfaces;
using Course.Infrastructure.Data;
using Course.Infrastructure.Data.Repositories;
using Course.Infrastructure.Implements;
using Marten;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Course.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			// Add infrastructure services here, e.g., database context, repositories, etc.
			var connectionString = configuration.GetConnectionString("Database");

			// DbContext (PostgreSQL)
			services.AddDbContext<AppDbContext, CourseDbContext>(opt =>
	opt.UseNpgsql(connectionString).EnableDetailedErrors().EnableSensitiveDataLogging());

			services.AddScoped<ICommandRepository<CourseEntity>, CommandRepository<CourseEntity>>();
			services.AddScoped<ICourseRepository, CourseRepository>();
			services.AddScoped<ICourseService, CourseService>();
			services.AddScoped<IUnitOfWork, UnitOfWork>();
			services.AddMarten(options =>
			{
				options.Connection(connectionString!);
			});

			return services;
		}
	}
}
