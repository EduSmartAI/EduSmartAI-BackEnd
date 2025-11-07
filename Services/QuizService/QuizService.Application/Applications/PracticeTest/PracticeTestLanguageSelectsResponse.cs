using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestLanguageSelectsResponse : AbstractApiResponse<List<PracticeTestLanguageSelectsResponseEntity>>
{
    public override List<PracticeTestLanguageSelectsResponseEntity> Response { get; set; }
}

public class PracticeTestLanguageSelectsResponseEntity
{
    public int LanguageId { get; set; }

    public string Name { get; set; } = null!;
}