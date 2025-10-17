using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public record QuizCourseAddQuestionsResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

