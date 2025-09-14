using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Commands.UpdateCourseModules;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Interfaces
{
	public interface ICourseService
	{
		Task<GetCoursesResponse> GetAllAsync(PaginationRequest pagination, CourseQuery? query = null, CancellationToken ct = default);

		Task<CreateCourseResponse> CreateAsync(CreateCourseDto dto, CancellationToken ct = default);

		Task<UpdateCourseResponse> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default);

		Task<UpdateCourseModulesResponse> UpdateCourseModulesAsync(Guid courseId, UpdateCourseModulesDto dto, CancellationToken ct = default);

		Task<GetCourseByIdForGuestResponse> GetCourseByIdForGuestAsync(Guid Id, CancellationToken ct = default);
		Task<GetCourseByIdForLectureResponse> GetCourseByIdForLectureAsync(Guid Id, CancellationToken ct = default);
	}
}
