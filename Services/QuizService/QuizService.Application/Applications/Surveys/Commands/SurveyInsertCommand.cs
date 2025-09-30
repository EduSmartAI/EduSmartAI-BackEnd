using System.ComponentModel.DataAnnotations;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Surveys.Commands;

public class SurveyInsertCommand : ICommand<SurveyInsertResponse>
{
    [Required(ErrorMessage = "Title is required")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Description is required")]
    public string? Description { get; set; }
    
    [Required(ErrorMessage = "SurveyCode is required")]
    public string SurveyCode { get; set; } = null!;
    
    [Required(ErrorMessage = "Questions are required")]
    public List<SurveyQuestionRequest> Questions { get; set; } = null!;
}

public record SurveyQuestionRequest
{
    
    [Required(ErrorMessage = "QuestionText is required")]
    public string QuestionText { get; set; } = null!;
    
    [Required(ErrorMessage = "QuestionType is required")]
    public short QuestionType { get; set; }

    [Required(ErrorMessage = "Answers are required")]
    public List<SurveyAnswerRequest> Answers { get; set; } = null!;
}

public record SurveyAnswerRequest
{
    [Required(ErrorMessage = "AnswerText is required")]
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
    
    public List<AnswerRuleRequest>? AnswerRules { get; set; }
}

public class AnswerRuleRequest
{
    public int? NumericMin { get; set; }
    public int? NumericMax { get; set; }
    public ConstantEnum.AnswerRuleUnit Unit { get; set; }
    public string? MappedField { get; set; }
    public string? Formula { get; set; }
}