using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.CreateCourse
{
	public class CreateCourseHandler(ICourseService courseService)
	: ICommandHandler<CreateCourseCommand, CreateCourseResponse>
	{
		public async Task<CreateCourseResponse> Handle(CreateCourseCommand request, CancellationToken ct)
		{
			return await courseService.CreateAsync(request.Payload, ct);
		}
	}
}
