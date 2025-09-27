using Course.Application.DTOs.UserLessonProgressDTO;
using Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress;
using Course.Application.UserLessonProgresses.Commands.EnrollCourse;
using Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress;
using Course.Application.UserLessonProgresses.Queries.CheckEnrollment;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseIdForStudents;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseSlugForStudents;

namespace Course.Application.Interfaces
{
	public interface IStudentProgressService
	{
		Task<CheckEnrollmentResponse> CheckEnrollmentAsync(Guid courseId, CancellationToken ct = default);

		Task<EnrollInCourseResponse> EnrollCourseAsync(Guid courseId, CancellationToken ct = default);

		Task<GetDetailsProgressByCourseIdForStudentResponse> GetCourseByIdForStudentAsync(Guid courseId, CancellationToken ct = default);

		Task<GetDetailsProgressByCourseSlugForStudentResponse> GetCourseBySlugForStudentAsync(string courseSlug, CancellationToken ct = default);

		Task<CreateUserLessonProgressResponse> CreateUserLessonProgressAsync(CreateUserLessonProgressDto dto, CancellationToken ct = default);

		Task<UpdateUserLessonProgressResponse> UpdateUserLessonProgressAsync(UpdateUserLessonProgressDto dto, CancellationToken ct = default);
	}
}
