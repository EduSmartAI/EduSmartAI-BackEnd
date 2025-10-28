using System.Text.Json;
using AuthService.Application.Consumers;
using AuthService.Infrastructure.Context;
using BaseService.Common.Utils;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AuthService.API;

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
        var logging = new LoggingUtil(_logger, "OutboxPublisher-AuthService");

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AuthServiceContext>();
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
                        case nameof(AccountCollectionEvent):
                            logging.InfoLog("Processing AccountCollectionEvent");
                            var e1 = JsonSerializer.Deserialize<AccountCollectionEvent>(e.Content);
                            await publishEndpoint.Publish(e1!, stoppingToken);
                            logging.InfoLog($"Successfully published AccountCollectionEvent for {e1!.Account.AccountId} quizzes");
                            break;
                        case nameof(StudentInsertEvent):
                            logging.InfoLog("Processing StudentInsertEvent");
                            var e2 = JsonSerializer.Deserialize<StudentInsertEvent>(e.Content);
                            await publishEndpoint.Publish(e2!, stoppingToken);
                            logging.InfoLog($"Successfully published StudentInsertEvent for StudentId: {e2!.UserId}");
                            break;
                        case nameof(LecturerInsertEvent):
                            logging.InfoLog("Processing LecturerInsertEvent");
                            var e3 = JsonSerializer.Deserialize<LecturerInsertEvent>(e.Content);
                            await publishEndpoint.Publish(e3!, stoppingToken);
                            logging.InfoLog($"Successfully published LecturerInsertEvent for LecturerId: {e3!.UserId}");
                            break;
                        case nameof(SendKeyEvent):
                            logging.InfoLog("Processing SendKeyEvent");
                            var e4 = JsonSerializer.Deserialize<SendKeyEvent>(e.Content);
                            await publishEndpoint.Publish(e4!, stoppingToken);
                            logging.InfoLog($"Successfully published SendKeyEvent for Key of Email: {e4.Email}: {e4!.Key}");
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