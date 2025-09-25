using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.GetCourseBySlug
{
	public class GetCourseBySlugForStudentHandler(ICourseService _courseService) : IQueryHandler<GetCourseBySlugForStudentQuery, GetCourseBySlugForStudentResponse>
	{
		public async Task<GetCourseBySlugForStudentResponse> Handle(GetCourseBySlugForStudentQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.GetCourseBySlugForStudentAsync(request.Slug, cancellationToken);
		}
	}
}
