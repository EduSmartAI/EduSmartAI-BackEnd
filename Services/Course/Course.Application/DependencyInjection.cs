using Microsoft.Extensions.DependencyInjection;

namespace Course.Application
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApplication(this IServiceCollection services)
		{
			// Add application services here, e.g., MediatR, AutoMapper, etc.
			return services;
		}
	}
}
