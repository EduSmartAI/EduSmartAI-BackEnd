using BaseService.Common.ApiEntities;
using MediatR;

namespace StudentService.Application.Applications.SuggestCourses.Commands.AcceptCourseSuggestion;

/// <summary>
/// Command to accept a course suggestion
/// </summary>
public class AcceptCourseSuggestionCommand : IRequest<AcceptCourseSuggestionResponse>
{ 
    public Guid CourseSuggestionId { get; set; }
}