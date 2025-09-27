using AiService.Application.Features.AiEvaluate;
using AiService.Application.Features.AiExternalCourse;
using AiService.Application.Interfaces;
using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using MassTransit;
using MediatR;

namespace AiService.Application.Handler
{
    public class AiRecommendHandler : IRequestHandler<AiEvaluateRequest, AiEvaluateResponse>
    {
        private readonly IAdvisorService _advisorService;
        private readonly IIdentityService _identityService;
        private readonly IPublishEndpoint _requestPublishEndpoint;
        private readonly IRequestClient<InsertLearningPathEventLearningPathEvent> _requestClient;
        private readonly IMediator _mediator;
        public AiRecommendHandler(IAdvisorService advisorService, IIdentityService identityService, IPublishEndpoint requestPublishEndpoint, IRequestClient<InsertLearningPathEventLearningPathEvent> requestClient, IMediator mediator)
        {
            _advisorService = advisorService;
            _identityService = identityService;
            _requestPublishEndpoint = requestPublishEndpoint;
            _requestClient = requestClient;
            _mediator = mediator;
        }
        public async Task<AiEvaluateResponse> Handle(AiEvaluateRequest request, CancellationToken cancellationToken)
        {
            // Get user info
            var currentUserEmail = _identityService.GetCurrentUser()!.Email;
            var studentId = _identityService.GetCurrentUser()!.UserId;

            // Insert Learning path
            //var learningPathId = Guid.NewGuid();
            //var response = await _requestClient.GetResponse<InsertLearningPathResponseEvent>(
            //    new InsertLearningPathEventLearningPathEvent(
            //        LearningPathId: learningPathId,
            //        PathName: "Lộ trình " + request.CareerGoal,
            //        StudentId: studentId,
            //        CurrentUserEmail: currentUserEmail
            //    ), cancellationToken);

            // AI recommend major
            var result = await _advisorService.EvaluateAsync(request, cancellationToken);
            var matched = result.Matched ?? new();
            var hasMatched = matched.Count > 0;

            //if (hasMatched)
            //    if (hasMatched && response.Message.Success)
            //    {
            //        // Publish message internal
            //        var majors = matched
            //            .Where(e => !string.IsNullOrWhiteSpace(e.MajorCode))
            //            .Select(e => new InternalMajorItem(
            //                MajorCode: e.MajorCode.Trim(),
            //                Reason: string.IsNullOrWhiteSpace(e.Reasons) ? "—" : e.Reasons.Trim(),
            //                SupportScore: e.SupportScore
            //            ))
            //            .ToList();

            //        await _requestPublishEndpoint.Publish(
            //            new InternalMajorEvent(
            //                LearningPathId: learningPathId,
            //                Majors: majors
            //            ),
            //            cancellationToken
            //        );
            //    }

            //if (response.Message.Success && (result.ExternalSuggestions?.Count ?? 0) > 0)
            if ((result.ExternalSuggestions?.Count ?? 0) > 0)
            {
                var externalMajors = result.ExternalSuggestions!
                    .Where(s => !string.IsNullOrWhiteSpace(s.MajorCode))
                    .Select(s => new ExternalMajorItem(
                        MajorCode: s.MajorCode!.Trim().ToUpperInvariant(),
                        Reason: string.IsNullOrWhiteSpace(s.WhyForYou) ? "—" : s.WhyForYou!.Trim(),
                        Description: string.IsNullOrWhiteSpace(s.Description) ? "—" : s.Description!.Trim(),
                        WhyForYou: string.IsNullOrWhiteSpace(s.WhyForYou) ? "—" : s.WhyForYou!.Trim()
                    ))
                    .GroupBy(x => x.MajorCode, StringComparer.OrdinalIgnoreCase)
                    .Select(g => g.First())
                    .Take(3)
                    .ToList();

                if (externalMajors.Count > 0)
                {
                    //var @event = new ExternalMajorEvent(
                    //    LearningPathId: Guid.Parse("19107229-e2cc-4387-8557-6a26f778c14b"), CurrentUserEmail: "",
                    //    Majors: externalMajors
                    //    );
                    //await _requestPublishEndpoint.Publish(@event, cancellationToken);

                    foreach (var m in externalMajors)
                    {
                        var extReq = new AiExternalCourseRequest
                        {
                            GoalMajor = m.MajorCode,
                            LearningPathId = "5d1c2d92-49e7-40b3-9e2a-5588885b29f0",
                            CurrentUserEmail = currentUserEmail,
                            MajorCode = m.MajorCode,
                            Reason = m.Reason
                        };
                        await _mediator.Send(extReq, cancellationToken);
                    }
                }
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
