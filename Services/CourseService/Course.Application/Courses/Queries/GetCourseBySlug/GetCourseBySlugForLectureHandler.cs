namespace Course.Application.Courses.Queries.GetCourseBySlug
{
	public class GetCourseBySlugForLectureHandler(ICourseService courseService) : IQueryHandler<GetCourseBySlugForLectureQuery, GetCourseBySlugForLectureResponse>
	{
		public async Task<GetCourseBySlugForLectureResponse> Handle(GetCourseBySlugForLectureQuery request, CancellationToken cancellationToken)
		{
			return await courseService.GetCourseBySlugForLectureAsync(request.Slug, cancellationToken);
		}
	}
}
