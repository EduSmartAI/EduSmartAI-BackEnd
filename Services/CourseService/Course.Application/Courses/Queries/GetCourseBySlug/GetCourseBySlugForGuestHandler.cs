using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.GetCourseBySlug
{
	public class GetCourseBySlugForGuestHandler(ICourseService courseService) : IQueryHandler<GetCourseBySlugForGuestQuery, GetCourseBySlugForGuestResponse>
	{
		public async Task<GetCourseBySlugForGuestResponse> Handle(GetCourseBySlugForGuestQuery request, CancellationToken cancellationToken)
		{
			return await courseService.GetCourseBySlugForGuestAsync(request.Slug, cancellationToken);
		}
	}
}
