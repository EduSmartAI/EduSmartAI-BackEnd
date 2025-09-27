using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.AIService.UpdateExternalMajorEvent;
using StudentService.Application.Applications.LearningGoals.Commands;

namespace StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor
{
    public class InsertLearningPathsMajorCommand : ICommand<LearningGoalInsertResponse>
    {
        public Guid PathId { get; set; }
        public string MajorCode { get; set; } = null!;
        public string? Reason { get; set; }
        public string? CurrentUserEmail { get; set; }
        public List<StepExternalMajorItem>? Courses { get; set; }

    }
}
