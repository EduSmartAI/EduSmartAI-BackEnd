using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Pagination;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.GetCoursesByLecture
{
	public record GetCoursesByLectureQuery(
		PaginationRequest Pagination,
		CourseQuery? Filter = null
	) : IQuery<GetCoursesByTeacherIdResponse>;

	public record GetCoursesByTeacherIdResponse : AbstractApiResponse<PaginatedResult<CourseDto>>
	{
		public override PaginatedResult<CourseDto> Response { get; set; }
	}
}
