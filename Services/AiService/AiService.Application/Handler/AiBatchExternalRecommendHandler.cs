using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AiService.Application.Handler
{
    public class AiBatchExternalRecommendRequest : IRequest<AiBatchExternalRecommendResponse>
    {
        public List<ExternalMajorRequestItem> Majors { get; set; } = new();
        public string LearningPathId { get; set; } = null!;
        public string CurrentUserEmail { get; set; } = null!;
    }

    public class ExternalMajorRequestItem
    {
        public string MajorCode { get; set; } = null!;
        public string Reason { get; set; } = null!;
    }

    public class AiBatchExternalRecommendResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class AiBatchExternalRecommendHandler(
        IAdvisorService advisorService,
        ILogger<AiBatchExternalRecommendHandler> logger,
        IRequestClient<UpdateBatchExternalMajorEvent> requestClient) 
        : IRequestHandler<AiBatchExternalRecommendRequest, AiBatchExternalRecommendResponse>
    {
        public async Task<AiBatchExternalRecommendResponse> Handle(AiBatchExternalRecommendRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var externalMajorItems = new List<ExternalMajorItem>();

                // Process all majors and collect roadmaps
                foreach (var major in request.Majors)
                {
                    try
                    {
                        var result = await advisorService.AskAsync(major.MajorCode, 80, true, cancellationToken);
                        logger.LogInformation("Advisor result for {MajorCode}: {Json}", major.MajorCode, JsonSerializer.Serialize(result));

                        var steps = result.Roadmap?.Steps
                            ?.Select((s, idx) => new StepExternalMajorItem(
                                Order: idx + 1,
                                Title: s.Title,
                                DurationWeeks: s.DurationWeeks,
                                Objectives: s.Objectives.ToList(),
                                SuggestedCourses: s.SuggestedCourses
                                    .Select(c => new StepCourseItem(
                                        Title: c.Title,
                                        Link: c.Link,
                                        Provider: c.Provider,
                                        Reason: c.Reason,
                                        Duration: c.EstDurationWeeks + " Tuần",
                                        Level: c.Level
                                    ))
                                    .ToList()))
                            .ToList() ?? new List<StepExternalMajorItem>();

                        externalMajorItems.Add(new ExternalMajorItem(
                            MajorCode: major.MajorCode,
                            Reason: major.Reason,
                            Steps: steps
                        ));
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to process external major {MajorCode}: {Message}", major.MajorCode, ex.Message);
                        // Continue processing other majors even if one fails
                    }
                }

                if (!externalMajorItems.Any())
                {
                    return new AiBatchExternalRecommendResponse
                    {
                        Success = false,
                        Message = "No external majors could be processed"
                    };
                }

                // Send batch request to StudentService
                var @event = new UpdateBatchExternalMajorEvent(
                    LearningPathId: Guid.Parse(request.LearningPathId),
                    CurrentUserEmail: request.CurrentUserEmail,
                    Majors: externalMajorItems
                );

                var response = await requestClient.GetResponse<UpdateBatchExternalMajorEventResponse>(@event, cancellationToken);
                
                if (!response.Message.Success)
                {
                    return new AiBatchExternalRecommendResponse
                    {
                        Success = false,
                        Message = "Failed to insert external majors"
                    };
                }

                return new AiBatchExternalRecommendResponse
                {
                    Success = true,
                    Message = $"Successfully processed {externalMajorItems.Count} external majors"
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AiBatchExternalRecommendHandler: {Message}", ex.Message);
                return new AiBatchExternalRecommendResponse
                {
                    Success = false,
                    Message = "Batch processing failed: " + ex.Message
                };
            }
        }
    }
}

