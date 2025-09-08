using BaseService.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using StudentService.API.Extensions;
using StudentService.Infrastructure.Contexts;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

// Configure core services
builder.Services.AddControllers();

// Configure services using extension methods
builder.Services.AddDatabaseServices();
builder.Services.AddAuthenticationServices();
builder.Services.AddRepositoryServices();
builder.Services.AddMessagingServices();
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();

builder.Services.AddDataProtection();
builder.Services.AddHttpContextAccessor();

#region MVC and API behavior configuration
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.SuppressModelStateInvalidFilter = true;
});
#endregion
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Configure(builder.Configuration.GetSection("Kestrel"));
});
#region Application build and middleware pipeline
var app = builder.Build();
await app.EnsureDatabaseCreatedAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
}

// Remove hardcoded URL configuration - let Docker handle port mapping
// app.Urls.Clear();
// app.Urls.Add("http://0.0.0.0:7002");
app.UseCors();
app.UseRouting();
app.UsePathBase("/student");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.UseSwagger();
app.UseSwaggerUI(settings =>
{
    settings.RoutePrefix = "swagger";
});
app.Run();
#endregion