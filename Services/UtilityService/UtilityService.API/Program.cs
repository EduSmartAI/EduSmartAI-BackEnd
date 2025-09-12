using BaseService.Common.Settings;
using Microsoft.OpenApi;
using UtilityService.API.Extensions;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// builder.Services.AddDatabaseServices();
// builder.Services.AddAuthenticationServices();
// builder.Services.AddRepositoryServices();
// builder.Services.AddMessagingServices();
builder.Services.AddSwaggerServices();
builder.Services.AddCorsServices();

var app = builder.Build();

await app.EnsureDatabaseCreatedAsync();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors();
app.UsePathBase("/utility");
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePages();
app.UseSwagger(c => c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0);
app.UseSwaggerUI(settings =>
{
    settings.RoutePrefix = "swagger";
});
app.MapControllers();
app.Run();