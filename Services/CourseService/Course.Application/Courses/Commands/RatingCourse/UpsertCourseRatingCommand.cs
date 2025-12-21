namespace Course.Application.Courses.Commands.RatingCourse
{
	public record UpsertCourseRatingCommand(Guid CourseId, short Rating) : ICommand<UpsertCourseRatingResponse>;

	public record UpsertCourseRatingResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get ; set ; }
	}

}
