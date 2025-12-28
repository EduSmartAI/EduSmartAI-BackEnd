using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestAdminInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}