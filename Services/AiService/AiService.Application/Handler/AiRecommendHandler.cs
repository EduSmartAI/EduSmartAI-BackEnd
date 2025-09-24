using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using MassTransit;
using MediatR;

namespace AiService.Application.Handler
{
    public class AiRecommendHandler : IRequestHandler<AiEvaluateRequest, AiEvaluateResponse>
    {
        private readonly IAdvisorService _advisorService;
        private readonly IPublishEndpoint _requestPublishEndpoint;
        public AiRecommendHandler(IAdvisorService advisorService, IPublishEndpoint requestPublishEndpoint)
        {
            _advisorService = advisorService;
            _requestPublishEndpoint = requestPublishEndpoint;
        }
        public async Task<AiEvaluateResponse> Handle(AiEvaluateRequest request, CancellationToken cancellationToken)
        {
            var result = await _advisorService.EvaluateAsync(request, cancellationToken);
            var matched = result.Matched ?? new();
            var hasMatched = matched.Count > 0;
            if (hasMatched)
            {
                var learningPathId = Guid.NewGuid();
                var majors = matched
                    .Where(e => !string.IsNullOrWhiteSpace(e.MajorCode))
                    .Select(e => new InternalMajorItem(
                        MajorCode: e.MajorCode.Trim(),
                        Reason: string.IsNullOrWhiteSpace(e.Reasons) ? "—" : e.Reasons.Trim(),
                        SupportScore: e.SupportScore
                    ))
                    .ToList();

                await _requestPublishEndpoint.Publish(
                    new InternalMajorEvent(
                        LearningPathId: learningPathId,
                        Majors: majors
                    ),
                    cancellationToken
                );
            }
            if (result == null)
            {
                throw new Exception("Error");
            }
            return new AiEvaluateResponse
            {
                Success = true,
                Message = "Uploaded successfully",
                Response = result
            };
        }
    }
}
