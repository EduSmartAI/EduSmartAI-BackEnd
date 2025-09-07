using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using OpenIddict.Validation.AspNetCore;

namespace StudentService.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        services.AddAuthentication(options =>
       {
           options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
       });

       // Configure token validation
       services.AddOpenIddict()
           .AddValidation(options =>
           {
               options.SetIssuer(Environment.GetEnvironmentVariable(ConstEnv.AuthServiceUrl)!);
               options.AddAudiences(Environment.GetEnvironmentVariable(ConstEnv.ClientId)!);

               options.UseIntrospection()
                   .AddAudiences(Environment.GetEnvironmentVariable(ConstEnv.ClientId)!)
                   .SetClientId(Environment.GetEnvironmentVariable(ConstEnv.ClientId)!)
                   .SetClientSecret(Environment.GetEnvironmentVariable(ConstEnv.ClientSecret)!);

               options.UseSystemNetHttp();
               options.UseAspNetCore();
           });
        return services;
    }
}