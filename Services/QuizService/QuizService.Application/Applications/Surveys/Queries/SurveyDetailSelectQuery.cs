using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;
using BuildingBlocks.Pagination;

namespace QuizService.Application.Applications.Surveys.Queries;

public record SurveyDetailSelectQuery : PaginationRequest, IQuery<SurveyDetailSelectResponse>
{
    [Required(ErrorMessage = "SurveyId is required")]
    public Guid SurveyId { get; set; }
    
    public int SemesterNumber { get; set; }
}