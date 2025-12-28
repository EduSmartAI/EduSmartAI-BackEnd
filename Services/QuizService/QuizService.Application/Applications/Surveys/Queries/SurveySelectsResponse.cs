using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Surveys.Queries;

public record SurveySelectsResponse : AbstractApiResponse<List<SurveySelectsResponseEntity>>
{
    public override List<SurveySelectsResponseEntity> Response { get; set; }
}

public record SurveySelectsResponseEntity
{
    public Guid SurveyId { get; set; }
    
    public string Title { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public string SurveyCode { get; set; } = null!;
}