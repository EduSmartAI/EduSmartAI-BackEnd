using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningGoals.Commands;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathInsertCommand : ICommand<LearningGoalInsertResponse>
{
    public required Guid PathId { get; set; }

    public required Guid StudentId { get; set; }

    public required string StudentEmail { get; set; }
    
    public required string PathName { get; set; }
    
    public required short Level { get; set; }
    
    public required string LevelReason { get; set; }
    
    public required bool IsSkipTest { get; set; }
    
    public required int LimitTime { get; set; }
    
    public required List<Guid> StudentSurveyIds { get; set; }
        
    public required Guid? StudentTestId { get; set; }
    
    public required List<Guid>? PracticeSubmissionIds { get; set; }
    
    public required string? EvaluationAndImprove { get; set; }
}