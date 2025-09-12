using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs;

namespace Course.Application.Interfaces
{
	public interface ICourseService
	{
		Task<GetCoursesResponse> GetAllAsync(PaginationRequest pagination, CourseQuery? query = null, CancellationToken ct = default);

		Task<CreateCourseResponse> CreateAsync(CreateCourseDto dto, CancellationToken ct = default);

		Task<UpdateCourseResponse> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default);

		Task<GetCourseByIdResponse> GetByIdAsync(Guid Id, CancellationToken ct = default);
	}
}
