using Mapster;
using MapsterMapper;
using StudentService.Application.Common.Mappings;

namespace StudentService.API.Extensions
{
    public static class MapsterExtensions
    {
        public static IServiceCollection AddApplicationMapster(this IServiceCollection services)
        {
            var config = TypeAdapterConfig.GlobalSettings;
            MapsterProfiles.Register(config);

            services.AddSingleton(config);
            services.AddSingleton<IMapper>(new Mapper(config));
            return services;
        }
    }
}
