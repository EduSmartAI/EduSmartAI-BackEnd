using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.UpdateCourse
{
	public class UpdateCourseHandler(ICourseService courseService)
	: ICommandHandler<UpdateCourseCommand, UpdateCourseResponse>
	{
		public async Task<UpdateCourseResponse> Handle(UpdateCourseCommand request, CancellationToken ct)
		{
			var updated = await courseService.UpdateAsync(request.CourseId, request.Payload, ct);

			return new UpdateCourseResponse
			{
				Success = true,
				Message = "Updated",
				Response = updated
			};
		}
	}
}
