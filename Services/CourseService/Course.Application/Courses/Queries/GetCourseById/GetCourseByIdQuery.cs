using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs;

namespace Course.Application.Courses.Queries.GetCourseById
{
	public record GetCourseByIdQuery(Guid Id) : IQuery<GetCourseByIdResponse>;

	public record GetCourseByIdResponse : AbstractApiResponse<CourseDetailDto>
	{
		public override CourseDetailDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}

}
