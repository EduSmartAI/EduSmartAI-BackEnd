using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestUserTemplateCodeSelectResponse : AbstractApiResponse<PracticeTestUserTemplateCodeSelectResponseEntity>
{
    public override PracticeTestUserTemplateCodeSelectResponseEntity Response { get; set; }
}

public class PracticeTestUserTemplateCodeSelectResponseEntity
{
    public string UserTemplateCode { get; set; } = null!;
}