using MediatR;

namespace AiService.Application.Features.AiEvaluate
{
    public class AiEvaluateRequest : IRequest<AiEvaluateResponse>
    {
        public string CareerGoal { get; set; } = "";
        public List<string> KnownFrameworks { get; set; } = [];
        public List<string> KnownLanguages { get; set; } = [];
        public string ExternalLimitTime { get; set; } = "";
        public int KRetrieval { get; set; } = 4;
        public int ScoreThreshold { get; set; } = 60;
    }
}
