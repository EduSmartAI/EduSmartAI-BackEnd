using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using OpenIddict.Validation.AspNetCore;

namespace ReverseProxy.Extensions;

public static class AuthenticationExtensions
{
    /// <summary>
    /// Configure OpenIddict Authentication for ReverseProxy
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddReverseProxyAuthentication(this IServiceCollection services,
        IConfiguration configuration)
    {
        // Load environment variables from .env file
        EnvLoader.Load();

        // Configure OpenIddict Validation
        services.AddAuthentication(options =>
        {
            options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                // Get authority from environment variables
                var authority = Environment.GetEnvironmentVariable(ConstEnv.AuthServiceUrl);
                options.SetIssuer(authority!);

                // Get audience from environment variables
                var audience = Environment.GetEnvironmentVariable(ConstEnv.JwtAudience);
                options.AddAudiences(audience!);

                // Get client credentials from environment variables
                var clientId = Environment.GetEnvironmentVariable(ConstEnv.ClientId);
                var clientSecret = Environment.GetEnvironmentVariable(ConstEnv.ClientSecret);

                // Use introspection endpoint to validate token
                options.UseIntrospection()
                    .AddAudiences(audience!)
                    .SetClientId(clientId!)
                    .SetClientSecret(clientSecret!);

                // Integrate with ASP.NET Core
                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        return services;
    }
}