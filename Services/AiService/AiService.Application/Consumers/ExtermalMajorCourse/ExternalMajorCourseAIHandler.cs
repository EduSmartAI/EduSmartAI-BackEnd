using AiService.Application.Features.ExtermalMajorCourse;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AiService.Application.Consumers.ExtermalMajorCourse
{
    public class ExternalMajorCourseAIHandler(
        IAdvisorService advisorService,
        ILogger<ExternalMajorCourseAIHandler> _logger,
        IRequestClient<UpdateExternalMajorEvent> _requestClient) : IRequestHandler<ExtermalMajorCourseRequest, ExtermalMajorCourseResponse>
    {
        public async Task<ExtermalMajorCourseResponse> Handle(ExtermalMajorCourseRequest request, CancellationToken cancellationToken)
        {
            //var result = await advisorService.AskAsync(request.GoalMajor, 80, true, cancellationToken) ?? throw new Exception("Error");
            //_logger.LogInformation("Advisor result json: {Json}", JsonSerializer.Serialize(result));

            //// Add steps from result gen by AI
            //var steps = result.Roadmap?.Steps
            //    ?.Select((s, idx) => new StepExternalMajorItem(
            //        Order: idx + 1,
            //        Title: s.Title ?? string.Empty,
            //        DurationWeeks: s.DurationWeeks,
            //        Objectives: (s.Objectives ?? new()).ToList(),
            //        SuggestedCourses: (s.SuggestedCourses ?? new())
            //            .Select(c => new StepCourseItem(
            //                Title: c.Title ?? string.Empty,
            //                Link: c.Link ?? string.Empty,
            //                Provider: c.Provider ?? string.Empty,
            //                Reason: c.Reason ?? string.Empty,
            //                Duration: c.EstDurationWeeks.ToString() + " Tuần",
            //                Level: c.Level
            //                ))
            //            .ToList()))
            //    .ToList() ?? new List<StepExternalMajorItem>();

            //// Send message event
            //var response = await _requestClient.GetResponse<UpdateExternalMajorEvent>(
            //    new UpdateExternalMajorEvent(
            //        LearningPathId: request.LearningPathId,
            //        Steps: steps
            //    ), cancellationToken);

            //return new ExtermalMajorCourseResponse
            //{
            //    Success = true,
            //    Message = "Uploaded successfully",
            //    Response = result
            //};
            return null;
        }
    }
}
