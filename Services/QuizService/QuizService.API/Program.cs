using BaseService.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using QuizService.API.Extensions;

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
    app.UseDeveloperExceptionPage();
}

app.UseCors();
app.UsePathBase("/quiz");
app.UseRouting();
app.UseAuthentication();
app.UseStatusCodePages();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();
app.UseSwagger(c => c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0);
app.UseSwaggerUI(settings =>
{
    settings.RoutePrefix = "swagger";
});
app.Run();
#endregion