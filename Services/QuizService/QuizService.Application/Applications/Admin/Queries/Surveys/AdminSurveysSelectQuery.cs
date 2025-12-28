using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Admin.Queries.Surveys;

public class AdminSurveysSelectQuery : IQuery<AdminSurveysSelectResponse>
{
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Filter by SurveyTypeId
    /// </summary>
    public short? SurveyTypeId { get; set; }
    
    /// <summary>
    /// Filter by SurveyCode
    /// </summary>
    public string? SurveyCode { get; set; }
    
    /// <summary>
    /// Search by Title
    /// </summary>
    public string? SearchTitle { get; set; }
}

