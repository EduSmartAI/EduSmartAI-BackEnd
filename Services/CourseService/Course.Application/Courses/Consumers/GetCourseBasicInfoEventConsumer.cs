using BuildingBlocks.Messaging.Events.StudentService;

namespace Course.Application.Courses.Consumers
{
	public class GetCourseBasicInfoEventConsumer(ICourseService _courseService) : IConsumer<GetCourseBasicInfoEvent>
	{
		public async Task Consume(ConsumeContext<GetCourseBasicInfoEvent> context)
		{
			var courseIds = context.Message.CourseIds;

			var result = await _courseService.GetBasicCoursesInforAsync(courseIds, context.CancellationToken);

			await context.RespondAsync(result);
		}
	}
}
