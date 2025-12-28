using BaseService.Common.Settings;
using PaymentService.API;
using PaymentService.Application;
using PaymentService.Infrastructure;

EnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddApplication()
	.AddApiServices();

var app = builder.Build();

await app.EnsureDatabaseCreatedAsync();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseApiServices();

app.Run();