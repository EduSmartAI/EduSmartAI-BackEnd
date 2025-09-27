using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseSlugForStudents
{
	public class GetDetailsProgressByCourseSlugForStudentHandler(IStudentProgressService _studentProgressService) : IQueryHandler<GetDetailsProgressByCourseSlugForStudentQuery, GetDetailsProgressByCourseSlugForStudentResponse>
	{
		public async Task<GetDetailsProgressByCourseSlugForStudentResponse> Handle(GetDetailsProgressByCourseSlugForStudentQuery request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.GetCourseBySlugForStudentAsync(request.Slug, cancellationToken);
		}
	}
}
