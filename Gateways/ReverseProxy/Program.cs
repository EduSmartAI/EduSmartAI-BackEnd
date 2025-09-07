using ReverseProxy.Authorizations;
using ReverseProxy.Configurations;
using ReverseProxy.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddOpenApi();

// Add Authentication with OpenIdConnect/JWT
builder.Services.AddReverseProxyAuthentication(builder.Configuration);

// Add YARP Reverse Proxy (routes & clusters)
builder.Services.AddReverseProxy()
    .LoadFromMemory(
        RouteConfiguration.GetRoutes(),
        ClusterConfiguration.GetClusters()
    )
    .ConfigureHttpClient((context, handler) =>
    {
        handler.AllowAutoRedirect = false;
    });

// Add Role Authorization service
builder.Services.AddSingleton<IRoleAuthorizationService, RoleAuthorizationService>();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseRouting();
app.Use(async (context, next) =>
{
    context.Request.EnableBuffering();
    context.Request.Body.Position = 0;
    await next();
});
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.ConfigureSwaggerUi();
}

app.MapControllers();
app.MapReverseProxy();
app.UseMiddleware<RoleAuthorizationMiddleware>();
app.Run();