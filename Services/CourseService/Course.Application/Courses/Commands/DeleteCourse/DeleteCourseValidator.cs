namespace Course.Application.Courses.Commands.DeleteCourse
{
	public class DeleteCourseValidator : AbstractValidator<DeleteCourseCommand>
	{
		public DeleteCourseValidator()
		{
			RuleFor(x => x.CourseId).NotEmpty().NotNull();
		}
	}
}
