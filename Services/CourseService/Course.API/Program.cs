using BaseService.Common.Settings;
using Course.API;
using Course.Application;
using Course.Infrastructure;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddApplication()
	.AddApiServices();

var app = builder.Build();

app.UseApiServices();

app.Run();
