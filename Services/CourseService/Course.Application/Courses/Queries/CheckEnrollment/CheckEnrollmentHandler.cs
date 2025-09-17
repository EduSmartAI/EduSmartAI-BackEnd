using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Queries.CheckEnrollment
{
	public class CheckEnrollmentHandler(ICourseService _courseService) : IQueryHandler<CheckEnrollmentQuery, CheckEnrollmentResponse>
	{
		public async Task<CheckEnrollmentResponse> Handle(CheckEnrollmentQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.CheckEnrollmentAsync(request.CourseId, cancellationToken);
		}
	}
}