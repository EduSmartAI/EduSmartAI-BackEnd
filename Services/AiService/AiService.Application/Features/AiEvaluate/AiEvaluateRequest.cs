using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;

namespace AiService.Application.Features.AiEvaluate
{
    public class AiEvaluateRequest : IRequest<AiEvaluateResponse>
    {
        public string CareerGoal { get; set; } = null!;
        public List<string> KnownFrameworks { get; set; } = [];
        public List<string> KnownLanguages { get; set; } = [];
        public string ExternalLimitTime { get; set; } = null!;
        public int KRetrieval { get; set; } = 4;
        public int ScoreThreshold { get; set; } = 60;
        
        public IdentityEntity IdentityEntity { get; set; }
        public Guid LearningPathId { get; set; }
        public Guid SemesterId { get; set; }
        
        public short StudentLevel { get; set; }
    }
}
