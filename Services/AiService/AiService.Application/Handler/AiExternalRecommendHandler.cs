using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiService.Application.Handler
{
    public class AiExternalRecommendHandler(
        IAdvisorService advisorService,
        ILogger<AiExternalRecommendHandler> logger,
        IRequestClient<UpdateExternalMajorEvent> requestClient,
        IPublishEndpoint requestPublishEndpoint) : IRequestHandler<AiExternalCourseRequest, AiExternalCourseResponse>
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
            var aiExternalCourseResponse = new AiExternalCourseResponse { Success = false };

            try
            {
                var result = await advisorService.AskAsync(request.GoalMajor, 80, true, cancellationToken);
                logger.LogInformation("Advisor result json: {Json}", JsonSerializer.Serialize(result));

                var steps = result.Roadmap?.Steps
                   ?.Select((s, idx) => new StepExternalMajorItem(
                       Order: idx + 1,
                       Title: s.Title,
                       DurationWeeks: s.DurationWeeks,
                       Objectives: (s.Objectives).ToList(),
                       SuggestedCourses: (s.SuggestedCourses)
                           .Select(c => new StepCourseItem(
                               Title: c.Title,
                               Link: c.Link,
                               Provider: c.Provider,
                               Reason: c.Reason,
                               Duration: c.EstDurationWeeks + " Tuần",
                               Level: c.Level
                               ))
                           .ToList()))
                   .ToList();

                var @event = new UpdateExternalMajorEvent(
                        LearningPathId: Guid.Parse(request.LearningPathId),
                        CurrentUserEmail: request.CurrentUserEmail,
                        MajorCode: request.MajorCode,
                        Reason: request.Reason,
                        Steps: steps ?? []);

                var response = await requestClient.GetResponse<UpdateExternalMajorEventResponse>(@event, cancellationToken);
                if (!response.Message.Success)
                {
                    aiExternalCourseResponse.SetMessage(MessageId.E00000, "Uploaded failed");
                    return aiExternalCourseResponse;
                }

                aiExternalCourseResponse.Success = true;
                aiExternalCourseResponse.SetMessage(MessageId.I00001, "Uploaded successfully");
                aiExternalCourseResponse.Response = result;
                return aiExternalCourseResponse;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AiExternalRecommendHandler: {Message}", ex.Message);
                aiExternalCourseResponse.SetMessage(MessageId.E00000, "Uploaded failed");
                return aiExternalCourseResponse;
            }
        }
    }
}
