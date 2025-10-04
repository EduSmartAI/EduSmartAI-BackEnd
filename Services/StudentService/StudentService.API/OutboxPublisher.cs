using System.Text.Json;
using BaseService.Common.Utils;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using NLog;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
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
                    var evt = JsonSerializer.Deserialize<StudentInformationUpdatedEvent>(e.Content);
                    await publishEndpoint.Publish(evt!, stoppingToken);

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