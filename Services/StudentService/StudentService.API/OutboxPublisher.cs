using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using StudentService.Application.Applications.Students.Consumers;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
using StudentService.Application.Applications.SuggestCourses.Consumers;
using StudentService.Infrastructure.Contexts;
using System.Text.Json;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.API;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly short _currentOutboxEnv;
    public OutboxPublisher(IServiceProvider services)
    {
        _services = services;
        var envValue = Environment.GetEnvironmentVariable(ConstEnv.OutboxEnvironment);
        _currentOutboxEnv = string.IsNullOrWhiteSpace(envValue)
            ? (short)OutboxEnvType.Production
            : (short)OutboxEnvType.Development;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var logging = new LoggingUtil(_logger, "OutboxPublisher-StudentService");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StudentServiceContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            var events = await db.OutboxMessages
                .Where(m => m.ProcessedOnUtc == null && m.OutboxEnvironment == _currentOutboxEnv)
                .ToListAsync(stoppingToken);

            foreach (var e in events)
            {
                try
                {
                    logging.InfoLog($"Processing event with Type: '{e.Type}' and Id: {e.Id}");

                    switch (e.Type)
                    {
                        case nameof(StudentInformationUpdatedEvent):
                            logging.InfoLog("Processing StudentInformationUpdatedEvent");
                            var e1 = JsonSerializer.Deserialize<StudentInformationUpdatedEvent>(e.Content);
                            await publishEndpoint.Publish(e1!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentInformationUpdatedEvent for StudentId: {e1!.Student.StudentId}");
                            break;
                        case nameof(SuggestCourseCollectionEvent):
                            logging.InfoLog("Processing SuggestCourseCollectionEvent");
                            var e2 = JsonSerializer.Deserialize<SuggestCourseCollectionEvent>(e.Content);
                            await publishEndpoint.Publish(e2!, stoppingToken);
                            logging.InfoLog($"Successfully published SuggestCourseCollectionEvent for StudentId: {e2.SuggestCourseCollections.First().StudentId}");
                            break;
                        case nameof(StudentCollectionEvent):
                            logging.InfoLog("Processing StudentCollectionEvent");
                            var e3 = JsonSerializer.Deserialize<StudentCollectionEvent>(e.Content);
                            await publishEndpoint.Publish(e3!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentCollectionEvent for StudentId: {e3.Student.StudentId}");
                            break;
                        default:
                            logging.WarningLog($"Unknown event type: {e.Type}");
                            break;
                    }
                    e.ProcessedOnUtc = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    logging.ErrorLog($"Failed to publish event with Id {e.Id}: {ex.Message}");
                }
            }

            await db.SaveChangesAsync(stoppingToken);
            await Task.Delay(3000, stoppingToken);
        }
    }

}