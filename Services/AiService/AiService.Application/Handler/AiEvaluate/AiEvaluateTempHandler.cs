using AiService.Application.Features.AiEvaluate;
using AiService.Application.Interfaces;
using MediatR;

namespace AiService.Application.Handler.AiEvaluate
{
    public class AiEvaluateTempHandler(IAdvisorService _advisorService) : IRequestHandler<AiEvaluateTempRequest, AiEvaluateResponse>
    {
        public async Task<AiEvaluateResponse> Handle(AiEvaluateTempRequest request, CancellationToken cancellationToken)
        {
            var evalReq = new AiEvaluateRequest
            {
                CareerGoal = request.CareerGoal,
                KnownFrameworks = request.KnownFrameworks,
                KnownLanguages = request.KnownLanguages,
                ExternalLimitTime = request.ExternalLimitTime,
                KRetrieval = request.KRetrieval,
                ScoreThreshold = request.ScoreThreshold,
                IdentityEntity = request.IdentityEntity,
                LearningPathId = request.LearningPathId,
                SemesterId = request.SemesterId,
                StudentLevel = request.StudentLevel,
                CourseImproves = request.CourseImproves,
            };

            // Gọi service
            var result = await _advisorService.EvaluateAsync(evalReq, cancellationToken);
            return new AiEvaluateResponse
            {
                Response = result,
                LearningPathId = request.LearningPathId
            };
        }
    }
}
