using BuildingBlocks.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace Course.Application
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplication(this IServiceCollection services)
		{
			// Add application services here, e.g., MediatR, AutoMapper, etc.

			// MediatR: quét toàn bộ assembly Application
			services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Course.Application.DependencyInjection).Assembly));

			// FluentValidation: quét validators trong Application
			services.AddValidatorsFromAssembly(typeof(Course.Application.DependencyInjection).Assembly);

			// Đăng ký pipeline ValidationBehavior cho mọi request MediatR
			services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

			return services;
		}
	}
}
