namespace Course.Application.Comments.Queries.GetCourseComments
{
	public class GetCourseCommentsValidator : AbstractValidator<GetCourseCommentsQuery>
	{
		public GetCourseCommentsValidator()
		{
			RuleFor(x => x.CourseId).NotNull().NotEmpty();
		}
	}
}
