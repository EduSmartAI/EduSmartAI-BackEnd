using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.LearningPaths;

public record StudentLevelCalculationResult : AbstractApiResponse<StudentLevelCalculationResultEntity>
{
    public override StudentLevelCalculationResultEntity Response { get; set; } = new();
}

public class StudentLevelCalculationResultEntity
{
    public short Level { get; set; }
    public List<string> PassedSubjects { get; set; } = new();
}

