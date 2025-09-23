using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.EnrollCourse
{
	public class EnrollCourseHandler(ICourseService courseService) : ICommandHandler<EnrollInCourseCommand, EnrollInCourseResponse>
	{
		public async Task<EnrollInCourseResponse> Handle(EnrollInCourseCommand request, CancellationToken cancellationToken)
		{
			return await courseService.EnrollCourseAsync(request.CourseId, cancellationToken);
		}
	}
}
