using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.AIService;
using Course.Domain.Models;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Course.Application.Consumers;

public class GetSubjectDetailConsumer(
    ICommandRepository<Subject> subjectRepository,
    ILogger<GetSubjectDetailConsumer> logger) : IConsumer<GetSubjectDetailEvent>
{
    public async Task Consume(ConsumeContext<GetSubjectDetailEvent> context)
    {
        var response = new GetSubjectDetailEventResponse();
        var subjectCode = NormalizeCode(context.Message.SubjectCode);

        if (string.IsNullOrWhiteSpace(subjectCode))
        {
            response.Success = false;
            response.Message = "SubjectCode is required.";
            await context.RespondAsync(response);
            return;
        }

        try
        {
            var subject = await subjectRepository.FirstOrDefaultAsync(
                x => x.IsActive && x.SubjectCode.ToUpper() == subjectCode,
                cancellationToken: context.CancellationToken);

            if (subject == null)
            {
                response.Success = false;
                response.Message = $"Subject {subjectCode} not found.";
            }
            else
            {
                response.Success = true;
                response.Response = new SubjectDetailDto
                {
                    SubjectCode = subject.SubjectCode,
                    SubjectTitle = subject.SubjectName,
                    SubjectDescription = subject.SubjectDescription ?? string.Empty
                };
                response.Message = "OK";
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load subject detail for code {SubjectCode}", subjectCode);
            response.Success = false;
            response.Message = "Failed to load subject detail.";
        }

        await context.RespondAsync(response);
    }

    private static string NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
}

