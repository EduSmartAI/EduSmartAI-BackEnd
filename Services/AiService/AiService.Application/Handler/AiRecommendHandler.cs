using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using MassTransit;
using MediatR;

namespace AiService.Application.Handler
{
    public class AiRecommendHandler : IRequestHandler<AiEvaluateRequest, AiEvaluateResponse>
    {
        private readonly IAdvisorService _advisorService;
        private readonly IPublishEndpoint _requestPublishEndpoint;
        private readonly IMediator _mediator;
        public AiRecommendHandler(IAdvisorService advisorService, IPublishEndpoint requestPublishEndpoint, IMediator mediator)
        {
            _advisorService = advisorService;
            _requestPublishEndpoint = requestPublishEndpoint;
            _mediator = mediator;
        }
        public async Task<AiEvaluateResponse> Handle(AiEvaluateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var insertLearningPathEvent = new InsertLearningPathEvent(
                    LearningPathId: request.LearningPathId,
                    PathName: "Lộ trình " + request.CareerGoal,
                    StudentId: request.IdentityEntity!.UserId,
                    CurrentUserEmail: request.IdentityEntity.Email
                );

                await _requestPublishEndpoint.Publish(insertLearningPathEvent, cancellationToken);

                //Console.WriteLine($"[PUBLISHER] Successfully published InsertLearningPathEvent for LearningPathId: {request.LearningPathId}");

                //// AI recommend major
                //var result = await _advisorService.EvaluateAsync(request, cancellationToken);
                //var matched = result.Matched;
                //var hasMatched = matched.Count > 0;

                //if (hasMatched)
                //{
                //    // Publish message internal
                //    var majors = matched
                //        .Where(e => !string.IsNullOrWhiteSpace(e.MajorCode))
                //        .Select(e => new InternalMajorItem(
                //            MajorCode: e.MajorCode.Trim(),
                //            Reason: string.IsNullOrWhiteSpace(e.Reasons) ? "—" : e.Reasons.Trim()
                //        ))
                //        .ToList();

                //    await _requestPublishEndpoint.Publish(
                //        new InternalMajorEvent(
                //            LearningPathId: request.LearningPathId,
                //            LimitTime: request.ExternalLimitTime,
                //            CurrentUserEmail: request.IdentityEntity.Email,
                //            Majors: majors,
                //            SemesterId: request.SemesterId
                //        ),
                //        cancellationToken
                //    );
                //}

                //if ((result.ExternalSuggestions?.Count ?? 0) > 0)
                //{
                //    var externalMajors = result.ExternalSuggestions!
                //        .Where(s => !string.IsNullOrWhiteSpace(s.MajorCode))
                //        .Select(s => new ExternalMajorItem(
                //            MajorCode: s.MajorCode!.Trim().ToUpperInvariant(),
                //            Reason: string.IsNullOrWhiteSpace(s.WhyForYou) ? "—" : s.WhyForYou!.Trim(),
                //            Description: string.IsNullOrWhiteSpace(s.Description) ? "—" : s.Description!.Trim(),
                //            WhyForYou: string.IsNullOrWhiteSpace(s.WhyForYou) ? "—" : s.WhyForYou!.Trim()
                //        ))
                //        .GroupBy(x => x.MajorCode, StringComparer.OrdinalIgnoreCase)
                //        .Select(g => g.First())
                //        .Take(3)
                //        .ToList();

                //    if (externalMajors.Count > 0)
                //    {
                //        foreach (var m in externalMajors)
                //        {
                //            try
                //            {
                //                var extReq = new AiExternalCourseRequest
                //                {
                //                    GoalMajor = m.MajorCode,
                //                    LearningPathId = request.LearningPathId.ToString(),
                //                    CurrentUserEmail = request.IdentityEntity.Email,
                //                    MajorCode = m.MajorCode,
                //                    Reason = m.Reason
                //                };
                //                await _mediator.Send(extReq, cancellationToken);
                //            }
                //            catch (Exception ex)
                //            {
                //                Console.WriteLine($"Failed to process external major {m.MajorCode}: {ex.Message}");
                //            }
                //        }
                //    }
                //}

                return new AiEvaluateResponse
                {
                    Success = true,
                    Message = "Generate successfully",
                    Response = null
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AiRecommendHandler: {ex.Message}");
                return new AiEvaluateResponse
                {
                    Success = false,
                    Message = "Generate failed: " + ex.Message
                };
            }
        }
    }
}
