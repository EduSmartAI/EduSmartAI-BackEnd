using BaseService.Application.Common;
using BaseService.Common.ApiEntities;
using BuildingBlocks.Pagination;

namespace QuizService.Application.Applications.Surveys.Queries;

public record SurveyDetailSelectResponse : AbstractApiResponse<PagedResult<SurveyDetailSelectResponseEntity>>
{
    public override PagedResult<SurveyDetailSelectResponseEntity> Response { get; set; }
}

public record SurveyDetailSelectResponseEntity
{
    public Guid SurveyId { get; set; }
    
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public string SurveyCode { get; set; } = null!;
    
    public List<QuestionSurveySelects> Questions { get; set; }

}

public record QuestionSurveySelects
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;

    public short QuestionType { get; set; }
    
    public List<AnswerSurveySelects>? Answers { get; set; }
}

public record AnswerSurveySelects
{
    public Guid AnswerId { get; set; }
    
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
}