using BaseService.Common.ApiEntities;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Features.AiExternalCourse
{
    public record AiExternalCourseResponse : AbstractApiResponse<AskResponse>
    {
        public override AskResponse Response { get; set; } = null!;
    }
}
