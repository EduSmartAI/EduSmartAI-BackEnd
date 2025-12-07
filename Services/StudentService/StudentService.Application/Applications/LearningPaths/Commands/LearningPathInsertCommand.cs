using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningGoals.Commands;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathInsertCommand : ICommand<LearningGoalInsertResponse>
{
    public Guid PathId { get; set; }

    public Guid StudentId { get; set; }

    public string StudentEmail { get; set; }
    
    public string PathName { get; set; }
    
    public short Level { get; set; }
    
    public string LevelReason { get; set; }
    
    public bool IsSkipTest { get; set; }
}