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
using PaymentService.Application.Interfaces;
using PaymentService.Domain.WriteModels;
using PaymentService.Infrastructure.Contexts;
using PaymentService.Infrastructure.Implements;
using StackExchange.Redis;
using Order = PaymentService.Domain.WriteModels.Order;
using SystemConfig = PaymentService.Domain.WriteModels.Systemconfig;
namespace PaymentService.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
		{
			var connectionString = Environment.GetEnvironmentVariable(ConstEnv.PaymentServiceDb);

			var redisConnectionString = Environment.GetEnvironmentVariable(ConstEnv.RedisCacheConnection)!;

			// C#
			services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConnectionString));
			services.AddScoped(sp => sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

			// DbContext (PostgreSQL)
			services.AddDbContext<AppDbContext, PaymentServiceContext>(opt =>
				opt.UseNpgsql(connectionString).EnableDetailedErrors().EnableSensitiveDataLogging());

			// Identity
			services.AddHttpContextAccessor();
			services.AddScoped<IIdentityService, IdentityService>();

			// Repositories
			services.AddScoped<ICommandRepository<Cart>, CommandRepository<Cart>>();
			services.AddScoped<ICommandRepository<CartItem>, CommandRepository<CartItem>>();
			services.AddScoped<ICommandRepository<SystemConfig>, CommandRepository<SystemConfig>>();
			services.AddScoped<ICommandRepository<PaymentTransaction>, CommandRepository<PaymentTransaction>>();
			services.AddScoped<ICommandRepository<Order>, CommandRepository<Order>>();
			services.AddScoped<ICommandRepository<OrderItem>, CommandRepository<OrderItem>>();

			// Services
			services.AddScoped<ICommonLogic, CommonLogic>();
			services.AddScoped<ICartService, CartService>();
			services.AddScoped<IPaymentServiceClient, PaymentServiceClient>();
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
			var db = scope.ServiceProvider.GetRequiredService<PaymentServiceContext>();
			await db.Database.EnsureCreatedAsync();
			return app;
		}
	}
}
