using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Settings;
using BaseService.Infrastructure.Repositories;
using Course.API;
using Course.Application;
using Course.Application.Consumers;
using Course.Application.Interfaces;
using Course.Application.Majors.Queries;
using Course.Application.Semesters.Queries;
using Course.Application.Subjects.Queries;
using Course.Domain.Models;
using Course.Infrastructure;
using Course.Infrastructure.Implements;

EnvLoader.Load();
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IMajorService, MajorService>();
builder.Services.AddScoped<ISemesterService, SemesterService>();

builder.Services.AddScoped<ICommandRepository<Major>, CommandRepository<Major>>();
builder.Services.AddScoped<ICommandRepository<Semester>, CommandRepository<Semester>>();

builder.Services.AddMediatR(x =>
{
	x.RegisterServicesFromAssemblyContaining<CourseMajorSemesterSelectQueryHandler>();
	x.RegisterServicesFromAssemblyContaining<SemesterSelectsQueryHandler>();
	x.RegisterServicesFromAssemblyContaining<MajorSelectsQueryHandler>();
	x.RegisterServicesFromAssemblyContaining<SubjectSelectsQueryHandler>();
});

builder.Services
	.AddInfrastructure(builder.Configuration)
	.AddApplication()
	.AddApiServices();

var app = builder.Build();

app.UseApiServices();

app.Run();
