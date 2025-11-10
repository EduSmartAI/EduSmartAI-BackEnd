using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.PracticeTest;

public record PracticeTestAdminLanguageInsertResponse : AbstractApiResponse<PracticeTestAdminLanguageInsertResponseEntity>
{
    public override PracticeTestAdminLanguageInsertResponseEntity Response { get; set; }
}

public class PracticeTestAdminLanguageInsertResponseEntity
{
    public int TotalLanguagesFromJudge0 { get; set; }
    
    public int ExistingLanguages { get; set; }
    
    public int NewLanguagesInserted { get; set; }
}