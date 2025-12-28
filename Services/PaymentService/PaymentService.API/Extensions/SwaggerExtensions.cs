using BaseService.API;
using Microsoft.OpenApi.Models;

namespace PaymentService.API.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddSwaggerServices(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Payment Service Swagger",
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
            // Add custom document filter to order operations by action name
            c.DocumentFilter<SwaggerOrderByActionFilter>();
        });
        
        return services;
    }
}