namespace BuildingBlocks.Messaging.Events.AIService.ModuleProgress
{
    public record GetModuleProgressEvents(Guid CourseId, Guid StudentId, Guid ModuleId);
}

