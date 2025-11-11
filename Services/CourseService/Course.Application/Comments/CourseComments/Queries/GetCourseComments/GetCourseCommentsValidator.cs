namespace Course.Application.Comments.CourseComments.Queries.GetCourseComments
{
	public class GetCourseCommentsValidator : AbstractValidator<GetCourseCommentsQuery>
	{
		public GetCourseCommentsValidator()
		{
			RuleFor(x => x.CourseId).NotNull().NotEmpty();
		}
	}
}
