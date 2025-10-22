using System.Text.Json;
using BaseService.Common.Utils;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
using StudentService.Application.Applications.SuggestCourses.Consumers;
using StudentService.Infrastructure.Contexts;

namespace StudentService.API;

public class OutboxPublisher : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public OutboxPublisher(IServiceProvider services)
    {
        _services = services;
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
                .Where(m => m.ProcessedOnUtc == null)
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