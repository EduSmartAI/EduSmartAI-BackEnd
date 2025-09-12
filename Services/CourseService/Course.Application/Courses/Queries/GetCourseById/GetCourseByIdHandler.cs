using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.GetCourseById
{
	public class GetCourseByIdHandler(ICourseService courseService) : IQueryHandler<GetCourseByIdQuery, GetCourseByIdResponse>
	{
		public async Task<GetCourseByIdResponse> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
		{
			return await courseService.GetByIdAsync(request.Id, cancellationToken);
		}
	}
}
