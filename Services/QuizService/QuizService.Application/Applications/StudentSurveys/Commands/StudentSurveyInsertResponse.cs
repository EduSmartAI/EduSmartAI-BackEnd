using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.StudentSurveys.Commands;

public record StudentSurveyInsertResponse : AbstractApiResponse<Guid?>
{
    public override Guid? Response { get; set; }
}