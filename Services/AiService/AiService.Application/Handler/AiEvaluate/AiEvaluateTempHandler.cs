using AiService.Application.Features.AiEvaluate;
using AiService.Application.Features.AiRecommend;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.QuizService;
using MediatR;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace AiService.Application.Handler.AiEvaluate
{
    public class AiEvaluateTempHandler(IAdvisorService advisorService) : IRequestHandler<AiEvaluationV2Request, AiEvaluateResponse>
    {
        public async Task<AiEvaluateResponse> Handle(AiEvaluationV2Request request, CancellationToken cancellationToken)
        {
            var evalReq = new AiEvaluateRequest
            {
                CareerGoal = request.CareerGoal,
                KnownFrameworks = request.KnownFrameworks ?? new List<string>(),
                KnownLanguages = request.KnownLanguages ?? new List<string>(),
                ExternalLimitTime = request.ExternalLimitTime,
                KRetrieval = request.KRetrieval,
                ScoreThreshold = request.ScoreThreshold,
                IdentityEntity = new IdentityEntity
                {
                    UserId = Guid.Empty,
                    Email = string.Empty
                },
                LearningPathId = request.LearningPathId,
                SemesterId = request.SemesterId,
                StudentLevel = request.StudentLevel,
                StudentMajor = new StudentMajor
                {
                    MajorCode = string.Empty,
                    MajorName = string.Empty
                },
                StudentPassedSubjects = null,
                CourseImproves = null,
                SubjectMarks = null,
                AbilityMarks = null,
                QuizSurvey = null,
                StudentTranscrpts = null,
                AbilityImprove = null
            };

            // Gọi service
            var result = await advisorService.EvaluateAsync(evalReq, cancellationToken);
            return new AiEvaluateResponse
            {
                Response = result,
                LearningPathId = request.LearningPathId
            };
        }
    }
}
