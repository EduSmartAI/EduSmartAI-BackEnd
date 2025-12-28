namespace Course.Application.UserLessonProgresses.Commands.EnrollCourse
{
	public record EnrollInCourseCommand(Guid CourseId) : ICommand<EnrollInCourseResponse>;

	public record EnrollInCourseResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
