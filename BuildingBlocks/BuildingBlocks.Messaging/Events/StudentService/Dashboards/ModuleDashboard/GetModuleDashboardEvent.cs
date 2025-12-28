namespace BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard
{
	public record GetModuleDashboardEvent(Guid StudentId, Guid CourseId);
}
