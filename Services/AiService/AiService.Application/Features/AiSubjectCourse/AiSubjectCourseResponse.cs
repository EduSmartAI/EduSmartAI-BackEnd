using BaseService.Common.ApiEntities;
using static AiService.Application.Contracts.AiRecommendContracts;

namespace AiService.Application.Features.AiSubjectCourse;

public record AiSubjectCourseResponse : AbstractApiResponse<SubjectCourseMatchResult>
{
    public override SubjectCourseMatchResult Response { get; set; } = new();
}

