using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public record StudentQuizCourseInsertResponse : AbstractApiResponse<StudentQuizCourseInsertResponseEntity>
{
    public override StudentQuizCourseInsertResponseEntity Response { get; set; }
}

public class StudentQuizCourseInsertResponseEntity
{
    public Guid StudentQuizCourseId { get; set; }
    
    public List<SuggestCourseEntity>? SuggestedCourses { get; set; }
}

public class SuggestCourseEntity
{
    public Guid SuggestCourseId { get; set; }
    
    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public int? DurationMinutes { get; set; }

    public short? Level { get; set; }
    
    public string CourseImageUrl { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}