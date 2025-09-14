using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.GetCourseById
{
	public record GetCourseByIdForGuestQuery(Guid Id) : IQuery<GetCourseByIdForGuestResponse>;

	public record GetCourseByIdForGuestResponse : AbstractApiResponse<CourseDetailForGuestDto>
	{
		public override CourseDetailForGuestDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}

}
