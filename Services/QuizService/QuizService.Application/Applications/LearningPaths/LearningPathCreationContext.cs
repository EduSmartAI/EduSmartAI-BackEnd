using BuildingBlocks.Messaging.Events.QuizService;
using QuizService.Domain.ReadModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Application.Applications.LearningPaths;

public class LearningPathCreationContext
{
    public required List<StudentQuizCollection> StudentQuizCollections { get; init; } = null!;
    public required IdentityEntity CurrentUser { get; init; } = null!;
    public required StudentInformationSelectsEventResponseEntity InformationResponse { get; init; } = null!;
    public required Guid LearningPathId { get; init; }
    public required int LimitTime { get; init; }
    public required short StudentLevel { get; init; }
    public required StudentMajor StudentMajor { get; set; }
    public List<CourseImproveContext>? CourseImprove { get; init; }
    public List<string>? StudentPassedSubjects { get; set; }
    
    public List<SubjectMarkContext>? SubjectMarks { get; set; }
    public List<AbilityMarkContext>? AbilityMarks { get; set; } 
    
    public List<StudentTranscriptContext>? StudentTranscripts { get; set; }
}

public class SubjectMarkContext
{
    public string SubjectCode { get; set; } = null!;
    public string SubjectName { get; set; } = null!;
    public double? Mark { get; set; }
}

public class AbilityMarkContext
{
    public required string Name { get; set; }
    public required double Mark { get; init; }
}

public class CourseImproveContext
{
    public string SubjectCode { get; set; } = null!;
    
    public string? SubjectPrerequisiteCode { get; set; }

    public int Level { get; set; }
}

public class StudentTranscriptContext
{
    public required string SubjectCode { get; set; }
    
    public required double? Mark { get; set; }
    
    public required string Status { get; set; }
}