using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.UpdateCourse
{
	public class UpdateCourseHandler(ICourseService courseService)
	: ICommandHandler<UpdateCourseCommand, UpdateCourseResponse>
	{
		public async Task<UpdateCourseResponse> Handle(UpdateCourseCommand request, CancellationToken ct)
		{
			return await courseService.UpdateAsync(request.CourseId, request.Payload, ct);
		}
	}
}
