using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestAdminUpdateResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

