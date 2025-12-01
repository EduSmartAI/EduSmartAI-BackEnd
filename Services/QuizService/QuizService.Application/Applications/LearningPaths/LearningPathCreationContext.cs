using BuildingBlocks.Messaging.Events.QuizService;
using QuizService.Domain.ReadModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Application.Applications.LearningPaths;

public class LearningPathCreationContext
{
    public List<StudentQuizCollection> StudentQuizCollections { get; init; } = null!;
    public IdentityEntity CurrentUser { get; init; } = null!;
    public StudentInformationSelectsEventResponseEntity InformationResponse { get; init; } = null!;
    public Guid LearningPathId { get; init; }
    public int LimitTime { get; init; }
    public short StudentLevel { get; init; }
    
    public List<CourseImproveContext> CourseImprove { get; init; }
    public List<string> StudentPassedSubjects { get; set; }
    
    public List<SubjectMarkContext>? SubjectMarks { get; set; }
    public List<AbilityMarkContext>? AbilityMarks { get; set; }
}

public class SubjectMarkContext
{
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public double? Mark { get; set; }
}

public class AbilityMarkContext
{
    public string Name { get; set; } = null!;
    public double Mark { get; set; }
}

public class CourseImproveContext
{
    public string SubjectCode { get; set; } = null!;
    
    public string? SubjectPrerequisiteCode { get; set; }

    public int Level { get; set; }
}