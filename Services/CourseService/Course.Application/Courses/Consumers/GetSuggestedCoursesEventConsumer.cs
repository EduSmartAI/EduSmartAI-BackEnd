using BuildingBlocks.Messaging.Events.StudentService;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Consumers
{
	public class GetSuggestedCoursesEventConsumer(ICourseService _courseService) : IConsumer<GetSuggestedCoursesEvent>
	{
		public async Task Consume(ConsumeContext<GetSuggestedCoursesEvent> context)
		{
			var dto = new GetSuggestedCoursesStudentServiceDto
			(
				context.Message.UserId,
				context.Message.SubjectCode,
				context.Message.CurrentLevel,
				context.Message.Type,
				context.Message.ExistingCourseIds
			);

			var result = await _courseService.GetSuggestedCoursesAsync(dto);

			await context.RespondAsync(result);
		}
	
	}
}
