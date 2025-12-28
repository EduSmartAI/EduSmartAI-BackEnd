using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public record StudentStudyTimeResponse : AbstractApiResponse<int>
{
    public override int Response { get; set; }
}
