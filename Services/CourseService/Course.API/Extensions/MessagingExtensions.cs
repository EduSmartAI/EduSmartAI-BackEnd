using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseMajorSemesterSelectEvents;
using Course.Application.Consumers;
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

				x.UsingRabbitMq((context, cfg) =>
				{
					cfg.Host(rabbitMqHost, "/", h =>
					{
						h.Username(rabbitMqUsername!);
						h.Password(rabbitMqPassword!);
					});

					cfg.ConfigureEndpoints(context);
				});
			});

			return services;
		}
	}
}
