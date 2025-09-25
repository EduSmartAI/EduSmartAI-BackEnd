using BaseService.Common.ApiEntities;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Features.ExtermalMajorCourse
{
    public record ExtermalMajorCourseResponse : AbstractApiResponse<AskResponse>
    {
        public override AskResponse Response { get; set; } = null!;
    }
}
