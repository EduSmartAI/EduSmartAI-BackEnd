using Microsoft.OpenApi.Models;

namespace NotificationService.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Notification Service Swagger",
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
            c.EnableAnnotations();
        });
        
        return services;
    }
}