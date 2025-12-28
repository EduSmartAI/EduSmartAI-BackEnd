using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public class RegenerateLearningPathEvent
{
    public required Guid LearningPathId { get; set; }
    
    public required Guid StudentId { get; set; }
    
    public required string StudentEmail { get; set; }
    
    public required short Level { get; set; }
    
    public required string LevelReason { get; set; }

    public required bool IsSkipTest { get; set; }
    
    public required int LimitTime { get; set; }
    
    public required Guid StudentMajorId { get; set; }
    
    public required Guid SemesterId { get; set; }
    
    public required List<string>? StudentPassedSubjects { get; set; }
    
    public required string? EvaluationAndImprove { get; set; }
    
    public required List<Guid> StudentSurveyIds { get; set; }
        
    public required Guid? StudentTestId { get; set; }
    
    public required List<Guid>? PracticeSubmissionIds { get; set; }

    public required List<RegenerateLearningPathEventTechnologies> Technologies { get; set; }
    
    public required RegenerateLearningPathEventLearningGoal LearningGoal { get; set; } 
    
    public required List<CourseImproveContext>? CourseImprove { get; set; }
    
    public required List<SubjectMarkContext>? SubjectMarks { get; set; }
    
    public required List<StudentTranscriptContext>? StudentTranscripts { get; set; }
    public List<AbilityImprove>? AbilityImprove { get; set; }
}

public class AbilityImprove
{
    public string Name { get; set; }
    
    public double Mark { get; set; }
}

public class StudentTranscriptContext
{
    public required string SubjectCode { get; set; }
    public required double? Mark { get; set; }
    public required string Status { get; set; }
}

public class SubjectMarkContext
{
    public required string SubjectCode { get; set; } = null!;
    public required string SubjectName { get; set; } = null!;
    public required double? Mark { get; set; }
}
public class CourseImproveContext
{
    public required string SubjectCode { get; set; } = null!;
    public required string? SubjectPrerequisiteCode { get; set; }
    public required int Level { get; set; }
}

public class RegenerateLearningPathEventLearningGoal
{
    public required Guid LearningGoalId { get; set; }
    public required string LearningGoalName { get; set; } = null!;
    public required short LearningGoalType { get; set; }
}

public class RegenerateLearningPathEventTechnologies
{
    public required Guid TechnologyId { get; set; }
    public required string TechnologyName { get; set; } = null!;
    public required short TechnologyType { get; set; }
}

public record RegenerateLearningPathEventResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}