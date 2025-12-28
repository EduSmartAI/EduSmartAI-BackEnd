using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Commands.UpdateCourseModules
{
	public record UpdateCourseModulesCommand(Guid CourseId, UpdateCourseModulesDto UpdateCourseModules)
		: ICommand<UpdateCourseModulesResponse>;

	public record UpdateCourseModulesResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
