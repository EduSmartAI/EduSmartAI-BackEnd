using Microsoft.OpenApi.Models;

namespace StudentService.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "User Service",
                Version = "v1"
            });
        
            c.AddSecurityDefinition("JWT_Token", new OpenApiSecurityScheme
            {
                Description = "Copy this into the value field: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey
            });
        
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "JWT_Token"
                        }
                    },
                    []
                }
            });
        });
        
        return services;
    }
}