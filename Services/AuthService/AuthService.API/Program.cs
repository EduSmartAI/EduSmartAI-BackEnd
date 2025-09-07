using AuthService.API;
using AuthService.API.Extensions;
using AuthService.API.Helpers;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using Microsoft.AspNetCore.Mvc;

// Load environment variables
EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

// Core services
builder.Services.AddControllers();
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<RoleSeederHostedService>();

// Configure services using extension methods
builder.Services.AddDatabaseServices();
builder.Services.AddRepositoryServices();
builder.Services.AddMessagingServices();
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();
builder.Services.AddAuthenticationServices();
builder.Services.AddHttpContextAccessor();

builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});

// Kestrel configuration
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(builder.Configuration.GetSection("Kestrel"));
});

var app = builder.Build();
app.UseForwardedHeaders();

// Ensure the database is created
await app.EnsureDatabaseCreatedAsync();

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

app.Urls.Clear();
app.Urls.Add("http://0.0.0.0:7001");
app.UseCors();
app.UsePathBase("/auth");
app.UseRouting();
app.UseAuthentication();
app.UseStatusCodePages();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(settings =>
{
    settings.RoutePrefix = "swagger";
});
app.Run();