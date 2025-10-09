using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public record StudentQuizCourseInsertResponse : AbstractApiResponse<Guid?>
{
    public override Guid? Response { get; set; }
}