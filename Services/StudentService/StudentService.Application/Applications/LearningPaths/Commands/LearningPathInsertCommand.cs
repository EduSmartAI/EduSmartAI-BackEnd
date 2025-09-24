using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningGoals.Commands;

namespace StudentService.Application.Applications.LearningPaths.Commands
{
    public class LearningPathInsertCommand : ICommand<LearningGoalInsertResponse>
    {
        public Guid PathId { get; set; }
    }
}
