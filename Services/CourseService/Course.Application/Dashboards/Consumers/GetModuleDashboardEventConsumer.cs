using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;

namespace Course.Application.Dashboards.Consumers
{
	public class GetModuleDashboardEventConsumer(IExternalCourseService _externalCourseService) : IConsumer<GetModuleDashboardEvent>
	{
		public async Task Consume(ConsumeContext<GetModuleDashboardEvent> context)
		{
			var evt = context.Message;
			var response = await _externalCourseService.GetCourseModuleDashboardAsync(evt.StudentId, evt.CourseId, context.CancellationToken);
			await context.RespondAsync(response);
		}
	}
}
