using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.SuggestCourses.Commands.AcceptCourseSuggestion;

/// <summary>
/// Response for accepting a course suggestion
/// </summary>
public record AcceptCourseSuggestionResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

