using BuildingBlocks.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace AiService.Application
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplication(this IServiceCollection services)
		{
			// Application Services
			services.AddValidatorsFromAssembly(typeof(AiService.Application.DependencyInjection).Assembly);

			// Đăng ký pipeline ValidationBehavior cho mọi request MediatR
			services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
			return services;
		}
	}
}
