using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Pagination;
using Course.Application.DTOs;

namespace Course.Application.Courses.Queries.GetCourses
{
	public record GetCoursesQuery(PaginationRequest Pagination, CourseQuery? Filter = null) : IQuery<GetCoursesResponse>;

	public record GetCoursesResponse : AbstractApiResponse<PaginatedResult<CourseDto>>
	{
		public override PaginatedResult<CourseDto> Response { get ; set ; }
	}
}
