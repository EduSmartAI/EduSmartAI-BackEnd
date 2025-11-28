using BuildingBlocks.Messaging.Events.AIService.SubjectInfoEvent;
using Course.Application.Interfaces;
using MassTransit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Course.Application.Consumers;

public class GetSubjectInfoConsumer(
    ISubjectInfoService subjectInfoService,
    ILogger<GetSubjectInfoConsumer> logger) : IConsumer<SubjectInfoEvent>
{
    public async Task Consume(ConsumeContext<SubjectInfoEvent> context)
    {
        var evt = context.Message;
        var response = new SubjectInfoEventResponse();

        var requestedSubjectCodes = (evt.SubjectCodes ?? new List<string>())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        try
        {
            var tasks = new List<Task<IReadOnlyList<SubjectInfoItem>>>();

            if (!string.IsNullOrWhiteSpace(evt.MajorCode))
            {
                tasks.Add(subjectInfoService.GetByMajorAsync(evt.MajorCode, context.CancellationToken));
            }

            if (requestedSubjectCodes.Count > 0)
            {
                tasks.Add(subjectInfoService.GetBySubjectCodesAsync(requestedSubjectCodes, context.CancellationToken));
            }

            if (tasks.Count == 0)
            {
                response.Success = false;
                response.Message = "MajorCode or SubjectCodes is required";
                await context.RespondAsync(response);
                return;
            }

            var results = await Task.WhenAll(tasks);
            var subjects = results
                .SelectMany(list => list)
                .GroupBy(item => item.SubjectCode, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();

            response.Success = subjects.Count > 0;
            response.Response = subjects;
            response.Message = subjects.Count > 0 ? "OK" : "No subjects found";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load subject information");
            response.Success = false;
            response.Message = "Failed to load subject info";
        }

        await context.RespondAsync(response);
    }
}
