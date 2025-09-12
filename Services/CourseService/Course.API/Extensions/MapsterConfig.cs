using Mapster;
using MapsterMapper;
using System.Reflection;

namespace Course.API.Extensions
{
	public static class MapsterConfig
	{
		public static IServiceCollection AddMapsterConfig(this IServiceCollection services)
		{
			var config = TypeAdapterConfig.GlobalSettings;
			config.Default.NameMatchingStrategy(NameMatchingStrategy.Flexible);
			config.Scan(Assembly.GetExecutingAssembly()); // quét IRegister trong assembly này

			services.AddSingleton(config);
			services.AddScoped<IMapper, ServiceMapper>();
			return services;
		}
	}
}
