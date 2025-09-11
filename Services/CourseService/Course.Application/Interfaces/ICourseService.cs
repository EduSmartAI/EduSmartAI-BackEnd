using BuildingBlocks.Pagination;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;

namespace Course.Application.Interfaces
{
	public interface ICourseService
	{
		Task<GetCoursesResponse> GetAllAsync(PaginationRequest pagination, CourseQuery? query = null, CancellationToken ct = default);

		Task<CourseDetailDto> CreateAsync(CreateCourseDto dto, CancellationToken ct = default);

		Task<CourseDetailDto> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default);
	}
}
