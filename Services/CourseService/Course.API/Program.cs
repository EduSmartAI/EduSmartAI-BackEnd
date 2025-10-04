using BaseService.Common.Settings;
using Course.API;
using Course.Application;
using Course.Infrastructure;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

//builder.Services.AddMediatR(x =>
//{
//	x.RegisterServicesFromAssemblyContaining<CourseMajorSemesterSelectQueryHandler>();
//	x.RegisterServicesFromAssemblyContaining<SemesterSelectsQueryHandler>();
//	x.RegisterServicesFromAssemblyContaining<MajorSelectsQueryHandler>();
//	x.RegisterServicesFromAssemblyContaining<SubjectSelectsQueryHandler>();
//});

builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddApplication()
	.AddApiServices();

var app = builder.Build();
await app.EnsureDatabaseCreatedAsync();
app.UseApiServices();

app.Run();
