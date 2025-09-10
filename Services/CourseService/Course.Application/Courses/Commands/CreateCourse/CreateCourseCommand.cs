using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs;

namespace Course.Application.Courses.Commands.CreateCourse
{
	public record CreateCourseCommand(CreateCourseDto Payload) : ICommand<CreateCourseResponse>;

	public record CreateCourseResponse : AbstractApiResponse<CourseDetailDto>
	{
		public override CourseDetailDto Response { get; set; } = default!;
	}
}
