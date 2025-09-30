namespace Course.Application.Courses.Queries.GetCourseById
{
	public class GetCourseByIdForGuestHandler(ICourseService courseService) : IQueryHandler<GetCourseByIdForGuestQuery, GetCourseByIdForGuestResponse>
	{
		public async Task<GetCourseByIdForGuestResponse> Handle(GetCourseByIdForGuestQuery request, CancellationToken cancellationToken)
		{
			return await courseService.GetCourseByIdForGuestAsync(request.Id, cancellationToken);
		}
	}
}
