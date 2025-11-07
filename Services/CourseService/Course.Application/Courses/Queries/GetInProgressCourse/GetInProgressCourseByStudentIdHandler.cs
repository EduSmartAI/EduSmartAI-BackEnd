namespace Course.Application.Courses.Queries.GetInProgressCourse
{
	public class GetInProgressCourseByStudentIdHandler(ICourseService _courseService) : IQueryHandler<GetInProgressCourseByStudentIdQuery, GetInProgressCourseByStudentIdResponse>
	{
		public async Task<GetInProgressCourseByStudentIdResponse> Handle(GetInProgressCourseByStudentIdQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.GetInProgressCourseByStudentIdAsync(request, cancellationToken);
		}
	}
}
