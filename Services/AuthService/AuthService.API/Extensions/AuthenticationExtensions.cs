using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using BaseService.Infrastructure.Contexts;
using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace AuthService.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddAuthenticationServices(this IServiceCollection services)
    {
        EnvLoader.Load();
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<AppDbContext>();
            })
            .AddServer(options =>
            {
                options.SetIssuer(new Uri(Environment.GetEnvironmentVariable(ConstEnv.AuthServiceUrl)!)); 
                ConfigureEndpoints(options);
                ConfigureFlows(options);
                ConfigureScopes(options);
                ConfigureCertificates(options);
                ConfigureTokenLifetime(options);
                ConfigureAspNetCore(options);
                
                options.AddEventHandler<OpenIddictServerEvents.ApplyTokenResponseContext>(builder =>
                {
                    builder.UseInlineHandler(context =>
                    {
                        var httpContext = context.Transaction.GetHttpRequest()?.HttpContext;
                        
                        if (httpContext != null)
                        {
                            var accessToken = context.Response.AccessToken;
                            var refreshToken = context.Response.RefreshToken;

                            if (!string.IsNullOrEmpty(accessToken))
                            {
                                httpContext.Response.Cookies.Append("access_token", accessToken, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTimeOffset.UtcNow.AddHours(1),
                                    Path = "/"
                                });
                            }

                            if (!string.IsNullOrEmpty(refreshToken))
                            {
                                httpContext.Response.Cookies.Append("refresh_token", refreshToken, new CookieOptions
                                {
                                    HttpOnly = true,
                                    Secure = true,
                                    SameSite = SameSiteMode.Strict,
                                    Expires = DateTimeOffset.UtcNow.AddHours(2),
                                    Path = "/"
                                });
                            }
                        }

                        return default;
                    });
                });
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        services.AddAuthorization();
        services.AddDataProtection();
        
        return services;
    }
    
    private static void ConfigureEndpoints(OpenIddictServerBuilder options)
    {
        options.SetTokenEndpointUris("/auth/connect/token");
        options.SetIntrospectionEndpointUris("/auth/connect/introspect");
        options.SetUserInfoEndpointUris("/auth/connect/userinfo");
        options.SetEndSessionEndpointUris("/auth/connect/logout");
        options.SetAuthorizationEndpointUris("/auth/connect/authorize");
    }
    
    private static void ConfigureFlows(OpenIddictServerBuilder options)
    {
        options.AllowCustomFlow("google");
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.AllowClientCredentialsFlow();
        options.AllowCustomFlow("logout");
        options.AllowCustomFlow("external");
        options.AllowAuthorizationCodeFlow().RequireProofKeyForCodeExchange();
    }
    
    private static void ConfigureScopes(OpenIddictServerBuilder options)
    {
        options.RegisterScopes(OpenIddictConstants.Scopes.OfflineAccess);
        options.RegisterScopes(
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile,
            OpenIddictConstants.Permissions.Scopes.Roles);
    }
    
    private static void ConfigureCertificates(OpenIddictServerBuilder options)
    {
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();
    }
    
    private static void ConfigureTokenLifetime(OpenIddictServerBuilder options)
    {
        options.UseReferenceAccessTokens();
        options.UseReferenceRefreshTokens();
        options.DisableAccessTokenEncryption();
        
        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(60));
        options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(120));
    }
    
    private static void ConfigureAspNetCore(OpenIddictServerBuilder options)
    {
        options.UseAspNetCore()
            .EnableTokenEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableAuthorizationEndpointPassthrough()
            .DisableTransportSecurityRequirement();
    }
}