using MediatR;

namespace AiService.Application.Features.AiSubjectCourse;

public class AiSubjectCourseRequest : IRequest<AiSubjectCourseResponse>
{
    public required string SubjectCode { get; set; }
    public string? SubjectTitle { get; set; }
    public string? SubjectDescription { get; set; }
    public int TopK { get; set; } = 10;
    public bool ShowSources { get; set; }
}

