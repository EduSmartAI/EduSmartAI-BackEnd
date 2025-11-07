using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using QuizService.Application.Applications.QuizCourses.Consumers;
using QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;
using QuizService.Infrastructure.Contexts;
using System.Text.Json;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace QuizService.API;

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
        var logging = new LoggingUtil(_logger, "OutboxPublisher-QuizService");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QuizServiceContext>();
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
                        case nameof(QuizCourseCollectionUpsertEvent):
                            logging.InfoLog("Processing QuizCourseCollectionInsertEvent");
                            var e3 = JsonSerializer.Deserialize<QuizCourseCollectionUpsertEvent>(e.Content);
                            await publishEndpoint.Publish(e3!, stoppingToken);
                            logging.InfoLog($"Successfully published QuizCourseCollectionInsertEvent for CourseId: {e3.Quiz.QuizId}");
                            break;
                        case nameof(StudentInterestSurveyAnalysisEvent):
                            logging.InfoLog("Processing StudentInterestSurveyAnalysisEvent");
                            var e4 = JsonSerializer.Deserialize<StudentInterestSurveyAnalysisEvent>(e.Content);
                            await publishEndpoint.Publish(e4!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentInterestSurveyAnalysisEvent for StudentId: {e4.StudentId}");
                            break;
                        case nameof(StudentMajorOrientationEvent):
                            logging.InfoLog("Processing StudentMajorOrientationEvent");
                            var e5 = JsonSerializer.Deserialize<StudentMajorOrientationEvent>(e.Content);
                            await publishEndpoint.Publish(e5!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentMajorOrientationEvent for StudentId: {e5.IdentityEntity.UserId}");
                            break;
                        case nameof(StudentQuizCourseInsertEvent):
                            logging.InfoLog("Processing StudentQuizCourseInsertEvent");
                            var e6 = JsonSerializer.Deserialize<StudentQuizCourseInsertEvent>(e.Content);
                            await publishEndpoint.Publish(e6!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentQuizCourseInsertEvent for QuizId: {e6.StudentQuiz.QuizId}");
                            break;
                        case nameof(QuizEvaluableCreatedEvent):
                            logging.InfoLog("Processing QuizEvaluableCreatedEvent");
                            var e8 = JsonSerializer.Deserialize<QuizEvaluableCreatedEvent>(e.Content);
                            await publishEndpoint.Publish(e8!, stoppingToken);
                            logging.InfoLog($"Successfully published QuizEvaluableCreatedEvent for QuizId: {e8!.QuizId}");
                            break;
                        case nameof(SuggestCourseForStudentEvent):
                            logging.InfoLog("Processing SuggestCourseForStudentEvent");
                            var e9 = JsonSerializer.Deserialize<SuggestCourseForStudentEvent>(e.Content);
                            await publishEndpoint.Publish(e9!, stoppingToken);
                            logging.InfoLog($"Successfully published SuggestCourseForStudentEvent for StudentId: {e9!.SuggestCourses.First().StudentId}");
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