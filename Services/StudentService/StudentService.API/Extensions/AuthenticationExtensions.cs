using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using OpenIddict.Validation.AspNetCore;

namespace StudentService.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        EnvLoader.Load();
        
        services.AddAuthentication(options =>
       {
           options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
       });

       // Configure token validation
       services.AddOpenIddict()
           .AddValidation(options =>
           {
               var authServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.AuthServiceUrl);
               var clientId = Environment.GetEnvironmentVariable(ConstEnv.ClientId);
               var clientSecret = Environment.GetEnvironmentVariable(ConstEnv.ClientSecret);
                   
               options.SetIssuer(authServiceUrl);
               options.AddAudiences(clientId);

               options.UseIntrospection()
                   .AddAudiences(clientId)
                   .SetClientId(clientId)
                   .SetClientSecret(clientSecret);

               options.UseSystemNetHttp();
               options.UseAspNetCore();
           });
        return services;
    }
}