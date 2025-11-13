using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using BaseService.Infrastructure.Identities;
using BaseService.Infrastructure.Repositories;
using JasperFx;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Infrastructure.Data;
using StackExchange.Redis;

namespace PaymentService.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services,
			IConfiguration configuration)
		{
			var connectionString = Environment.GetEnvironmentVariable(ConstEnv.PaymentServiceDb);

			var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;

			// C#
			services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
			services.AddScoped(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

			// DbContext (PostgreSQL)
			services.AddDbContext<AppDbContext, PaymentServiceDBContext>(opt =>
				opt.UseNpgsql(connectionString).EnableDetailedErrors().EnableSensitiveDataLogging());

			// Identity
			services.AddHttpContextAccessor();
			services.AddScoped<IIdentityService, IdentityService>();

			// Repositories


			// Services


			// Helpers


			// Unit of Work
			services.AddScoped<IUnitOfWork, UnitOfWork>();

			services.AddMarten(options =>
			{
				options.Connection(connectionString!);
				options.AutoCreateSchemaObjects = AutoCreate.All;
				options.DatabaseSchemaName = "PaymentServiceDB_Marten";
			});

			return services;
		}

		public static async Task<WebApplication> EnsureDatabaseCreatedAsync(this WebApplication app)
		{
			using var scope = app.Services.CreateScope();
			var db = scope.ServiceProvider.GetRequiredService<PaymentServiceDBContext>();
			await db.Database.EnsureCreatedAsync();
			return app;
		}
	}
}
