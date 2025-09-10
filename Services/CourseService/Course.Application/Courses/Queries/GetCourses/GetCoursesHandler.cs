using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.GetCourses
{
	public class GetCoursesHandler(ICourseService courseService) : IQueryHandler<GetCoursesQuery, GetCoursesResponse>
	{
		public async Task<GetCoursesResponse> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
		{
			return await courseService.GetAllAsync(request.Pagination, request.Filter, cancellationToken);
		}
	}
}
