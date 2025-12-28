using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;

namespace Course.Application.Dashboards.Consumers
{
	public class GetLessonDashboardEventConsumer(IExternalCourseService _externalCourseService) : IConsumer<GetLessonDashboardEvent>
	{
		public async Task Consume(ConsumeContext<GetLessonDashboardEvent> context)
		{
			var evt = context.Message;
			var response = await _externalCourseService.GetCourseLessonDashboardAsync(evt.StudentId, evt.CourseId, context.CancellationToken);
			await context.RespondAsync(response);
		}
	}
}
