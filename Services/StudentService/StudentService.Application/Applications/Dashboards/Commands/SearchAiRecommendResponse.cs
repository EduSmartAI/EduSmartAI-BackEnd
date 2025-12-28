using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Dashboards.Commands
{
    public record SearchAiRecommendResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; } = string.Empty;
    }
}

