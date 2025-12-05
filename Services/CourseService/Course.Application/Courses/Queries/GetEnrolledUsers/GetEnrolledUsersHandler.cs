namespace Course.Application.Courses.Queries.GetEnrolledUsers
{
	public class GetEnrolledUsersHandler(ICourseService _courseService) : IQueryHandler<GetEnrolledUsersQuery, GetEnrolledUsersResponse>
	{
		public async Task<GetEnrolledUsersResponse> Handle(GetEnrolledUsersQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.GetEnrolledUsersAsync(request, cancellationToken);
		}
	}
}
