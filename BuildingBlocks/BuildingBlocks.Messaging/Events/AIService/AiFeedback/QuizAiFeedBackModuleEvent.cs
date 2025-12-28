namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    public record QuizAiFeedBackModuleEvent(Guid CourseId, Guid StudentId, Guid ModuleId);
}
