using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Commands.UpdateCourse
{
	public record UpdateCourseCommand(Guid CourseId, UpdateCourseDto Payload)
	: ICommand<UpdateCourseResponse>;

	public record UpdateCourseResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
