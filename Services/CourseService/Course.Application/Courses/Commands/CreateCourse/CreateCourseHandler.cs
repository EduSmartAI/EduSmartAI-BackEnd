using BuildingBlocks.CQRS;
using Course.Application.Interfaces;

namespace Course.Application.Courses.Commands.CreateCourse
{
	public class CreateCourseHandler(ICourseService courseService)
	: ICommandHandler<CreateCourseCommand, CreateCourseResponse>
	{
		public async Task<CreateCourseResponse> Handle(CreateCourseCommand request, CancellationToken ct)
		{
			var created = await courseService.CreateAsync(request.Payload, ct);

			return new CreateCourseResponse
			{
				Success = true,
				Message = "Created",
				Response = created
			};
		}
	}
}
