namespace Course.Application.Courses.Queries.GetMyCourseRating
{
	public record GetMyCourseRatingQuery(Guid CourseId) : IQuery<GetMyCourseRatingResponse>;

	public record GetMyCourseRatingResponse : AbstractApiResponse<GetMyCourseRatingDto>
	{
		public override GetMyCourseRatingDto Response { get ; set ; }
	}

	public class GetMyCourseRatingDto
	{
		public bool IsRatedByCurrentUser { get; set; }
	}
}
