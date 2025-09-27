using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.UserLessonProgresses.Commands.EnrollCourse
{
	public class EnrollCourseHandler(IStudentProgressService _studentProgressService) : ICommandHandler<EnrollInCourseCommand, EnrollInCourseResponse>
	{
		public async Task<EnrollInCourseResponse> Handle(EnrollInCourseCommand request, CancellationToken cancellationToken)
		{
			return await _studentProgressService.EnrollCourseAsync(request.CourseId, cancellationToken);
		}
	}
}
