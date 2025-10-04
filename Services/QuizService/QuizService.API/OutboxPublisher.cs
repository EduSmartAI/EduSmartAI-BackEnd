using System.Text.Json;
using BaseService.Common.Utils;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using QuizService.Application.Applications.QuizCourses.Consumers;
using QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;
using QuizService.Infrastructure.Contexts;

namespace QuizService.API;

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
        var logging = new LoggingUtil(_logger, "OutboxPublisher-QuizService");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QuizServiceContext>();
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
                        case nameof(StudentQuizCollectionInsertEvent):
                            logging.InfoLog("Processing StudentQuizCollectionInsertEvent");
                            var e1 = JsonSerializer.Deserialize<StudentQuizCollectionInsertEvent>(e.Content);
                            await publishEndpoint.Publish(e1!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentQuizCollectionInsertEvent for {e1.StudentQuizzes} quizzes");
                            break;
                        case nameof(StudentMajorSemesterInformationEvent):
                            logging.InfoLog("Processing StudentMajorSemesterInformationEvent");
                            var e2 = JsonSerializer.Deserialize<StudentMajorSemesterInformationEvent>(e.Content);
                            await publishEndpoint.Publish(e2!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentMajorSemesterInformationEvent for StudentId: {e2}");
                            break;
                        case nameof(QuizCourseCollectionInsertEvent):
                            logging.InfoLog("Processing QuizCourseCollectionInsertEvent");
                            var e3 = JsonSerializer.Deserialize<QuizCourseCollectionInsertEvent>(e.Content);
                            await publishEndpoint.Publish(e3!, stoppingToken);
                            logging.InfoLog($"Successfully published QuizCourseCollectionInsertEvent for CourseId: {e3.Quiz.QuizId}");
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
            await Task.Delay(2000, stoppingToken);
        }
    }
}