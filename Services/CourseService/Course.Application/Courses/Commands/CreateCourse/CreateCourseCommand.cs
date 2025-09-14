using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Commands.CreateCourse
{
	public record CreateCourseCommand(CreateCourseDto Payload) : ICommand<CreateCourseResponse>;

	public record CreateCourseResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
