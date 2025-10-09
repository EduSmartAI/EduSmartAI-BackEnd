namespace Course.Application.Courses.Commands.DeleteCourse
{
	public record DeleteCourseCommand(Guid CourseId) : ICommand<DeleteCourseResponse>;
	public record DeleteCourseResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = default!;
	}
}
