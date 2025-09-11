using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs;

namespace Course.Application.Courses.Commands.UpdateCourse
{
	public record UpdateCourseCommand(Guid CourseId, UpdateCourseDto Payload)
	: ICommand<UpdateCourseResponse>;

	public record UpdateCourseResponse : AbstractApiResponse<CourseDetailDto>
	{
		public override CourseDetailDto Response { get; set; } = default!;
	}
}
