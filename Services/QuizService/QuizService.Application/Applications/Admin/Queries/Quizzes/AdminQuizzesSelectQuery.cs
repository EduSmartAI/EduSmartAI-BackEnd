using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.Quizzes;

/// <summary>
/// Query to select all quizzes/surveys for admin management
/// </summary>
public record AdminQuizzesSelectQuery : IQuery<AdminQuizzesSelectResponse>
{
    public int PageNumber { get; init; } = 1;
    
    public int PageSize { get; init; } = 10;
    
    public ConstantEnum.TestType? QuizType { get; init; }
    
    public Guid? SubjectCode { get; init; }
    
    public string? SurveyCode { get; init; }
}

