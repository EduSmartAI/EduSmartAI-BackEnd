using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.GetCourseById
{
	public class GetCourseByIdForStudentHandler(ICourseService _courseService) : IQueryHandler<GetCourseByIdForStudentQuery, GetCourseByIdForStudentResponse>
	{
		public async Task<GetCourseByIdForStudentResponse> Handle(GetCourseByIdForStudentQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.GetCourseByIdForStudentAsync(request.Id, cancellationToken);
		}
	}
}
