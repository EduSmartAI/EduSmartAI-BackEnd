using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Handler
{
    public class AiExternalRecommendHandler(
        IAdvisorService advisorService,
        ILogger<AiExternalRecommendHandler> _logger,
        IRequestClient<UpdateExternalMajorEvent> _requestClient,
        IPublishEndpoint _requestPublishEndpoint) : IRequestHandler<AiExternalCourseRequest, AiExternalCourseResponse>
    {
        /// <summary>
        /// Handle Generate course external from external Major
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public async Task<AiExternalCourseResponse> Handle(AiExternalCourseRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await advisorService.AskAsync(request.GoalMajor, 80, true, cancellationToken);
                _logger.LogInformation("Advisor result json: {Json}", JsonSerializer.Serialize(result));
                if (result == null)
                {
                    return new AiExternalCourseResponse
                    {
                        Success = false,
                        Message = "There's no result",
                        Response = new AskResponse()
                    };
                }
                var steps = result.Roadmap?.Steps
                   ?.Select((s, idx) => new StepExternalMajorItem(
                       Order: idx + 1,
                       Title: s.Title ?? string.Empty,
                       DurationWeeks: s.DurationWeeks,
                       Objectives: (s.Objectives ?? new()).ToList(),
                       SuggestedCourses: [.. (s.SuggestedCourses ?? new())
                           .Select(c => new StepCourseItem(
                               Title: c.Title ?? string.Empty,
                               Link: c.Link ?? string.Empty,
                               Provider: c.Provider ?? string.Empty,
                               Reason: c.Reason ?? string.Empty,
                               Duration: c.EstDurationWeeks.ToString() + " Tuần",
                               Level: c.Level
                               ))]))
                   .ToList() ?? [];

                var @event = new UpdateExternalMajorEvent(
                        LearningPathId: Guid.Parse(request.LearningPathId),
                        CurrentUserEmail: request.CurrentUserEmail,
                        MajorCode: request.MajorCode,
                        Reason: request.Reason,
                        Steps: steps);

                var response = await _requestClient.GetResponse<UpdateExternalMajorEventResponse>(@event, cancellationToken);
                //await _requestPublishEndpoint.Publish(@event, cancellationToken);
                if (!response.Message.Success)
                {
                    return new AiExternalCourseResponse
                    {
                        Success = false,
                        Message = "There's something error",
                        Response = new AskResponse()
                    };
                }

                return new AiExternalCourseResponse
                {
                    Success = true,
                    Message = "Generate successfully",
                    Response = result
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                throw;
            }
        }
    }
}
