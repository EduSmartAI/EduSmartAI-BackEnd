using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestAdminTestcasesInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}

