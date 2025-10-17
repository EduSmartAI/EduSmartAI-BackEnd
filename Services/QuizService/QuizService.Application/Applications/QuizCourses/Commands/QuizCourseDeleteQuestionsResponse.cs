using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Commands;

public record QuizCourseDeleteQuestionsResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

