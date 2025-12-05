using BuildingBlocks.Messaging.Events.AIService;
using Course.Application.Interfaces;
using MassTransit;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Course.Application.Consumers;

public class GetSubjectSemesterConsumer(
    ISubjectInfoService subjectInfoService,
    ILogger<GetSubjectSemesterConsumer> logger) : IConsumer<GetSubjectSemesterEvent>
{
    public async Task Consume(ConsumeContext<GetSubjectSemesterEvent> context)
    {
        var response = new GetSubjectSemesterEventResponse();
        var payload = context.Message.SubjectAndMajor;

        var normalizedMajor = NormalizeCode(payload?.MajorCode);
        var requestedCodes = (payload?.SubjectCodes ?? new List<string>())
            .Select(NormalizeCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (string.IsNullOrWhiteSpace(normalizedMajor) || requestedCodes.Count == 0)
        {
            response.Success = false;
            response.Message = "MajorCode and SubjectCodes are required.";
            await context.RespondAsync(response);
            return;
        }

        try
        {
            var subjects = await subjectInfoService.GetByMajorAsync(normalizedMajor, context.CancellationToken);
            var filterSet = requestedCodes.ToHashSet(StringComparer.OrdinalIgnoreCase);

            var data = subjects
                .Where(item => filterSet.Contains(item.SubjectCode))
                .Select(item => new SubjectSemesterInfo
                {
                    MajorCode = normalizedMajor,
                    SubjectCode = item.SubjectCode,
                    SemesterIndex = item.SemesterIndex,
                    SubjectIndex = item.SubjectIndex
                })
                .ToList();

            response.Success = data.Count > 0;
            response.Message = data.Count > 0 ? "OK" : "No subjects found.";
            response.Response = data;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to resolve subject semesters for major {MajorCode}", normalizedMajor);
            response.Success = false;
            response.Message = "Failed to load subject semester info.";
        }

        await context.RespondAsync(response);
    }

    private static string NormalizeCode(string? code) =>
        string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToUpperInvariant();
}

