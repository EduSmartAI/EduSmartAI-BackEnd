using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public class StudentSurveyLatestSelectQuery : IQuery<StudentSurveyLatestSelectQueryResponse>
{
    
}

public record StudentSurveyLatestSelectQueryResponse : AbstractApiResponse<List<StudentSurveySelectDetailResponseEntity>>
{
    public override List<StudentSurveySelectDetailResponseEntity> Response { get; set; }
}