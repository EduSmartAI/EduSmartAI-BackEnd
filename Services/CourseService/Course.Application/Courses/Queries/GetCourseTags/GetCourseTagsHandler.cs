namespace Course.Application.Courses.Queries.GetCourseTags
{
	public class GetCourseTagsHandler(ICourseService _courseService) : IQueryHandler<GetCourseTagsQuery, GetCourseTagsResponse>
	{
		public async Task<GetCourseTagsResponse> Handle(GetCourseTagsQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.GetCourseTagsAsync(cancellationToken);
		}
	}
}
