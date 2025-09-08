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

			return services;
		}
	}
}
