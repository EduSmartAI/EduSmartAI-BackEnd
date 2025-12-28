namespace Course.Application.Courses.Queries.GetCourseById
{
	public class GetCourseByIdForLectureHandler(ICourseService courseService) : IQueryHandler<GetCourseByIdForLectureQuery, GetCourseByIdForLectureResponse>
	{
		public async Task<GetCourseByIdForLectureResponse> Handle(GetCourseByIdForLectureQuery request, CancellationToken cancellationToken)
		{
			return await courseService.GetCourseByIdForLectureAsync(request.Id, cancellationToken);
		}
	}
}
