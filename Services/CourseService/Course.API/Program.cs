using Course.API;
using Course.Application;
using Course.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddApplication()
	.AddApiServices();

var app = builder.Build();

app.UseApiServices();

app.Run();
