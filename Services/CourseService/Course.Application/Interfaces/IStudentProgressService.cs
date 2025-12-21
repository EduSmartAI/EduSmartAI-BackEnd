using Course.Application.Courses.Commands.RatingCourse;
using Course.Application.DTOs.UserLessonProgressDTO;
using Course.Application.UserLessonProgresses.Commands.EnrollCourse;
using Course.Application.UserLessonProgresses.Commands.UpsertUserLessonProgress;
using Course.Application.UserLessonProgresses.Queries.CheckEnrollment;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseIdForStudents;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseSlugForStudents;
using Course.Application.UserLessonProgresses.Queries.GetMyLearningCourses;

namespace Course.Application.Interfaces
{
	public interface IStudentProgressService
	{
		Task<CheckEnrollmentResponse> CheckEnrollmentAsync(Guid courseId, CancellationToken ct = default);
		Task<CheckEnrollmentResponse> CheckEnrollmentExternalServiceAsync(Guid courseId, Guid userId, CancellationToken ct = default);

		Task<EnrollInCourseResponse> EnrollCourseAsync(Guid courseId, CancellationToken ct = default);

		Task<GetDetailsProgressByCourseIdForStudentResponse> GetCourseByIdForStudentAsync(Guid courseId, CancellationToken ct = default);

		Task<GetDetailsProgressByCourseSlugForStudentResponse> GetCourseBySlugForStudentAsync(string courseSlug, CancellationToken ct = default);

		Task<UpsertUserLessonProgressResponse> UpsertUserLessonProgressAsync(Guid lessonId, UpsertUserLessonProgressDto dto, CancellationToken ct = default);

		Task<GetMyLearningCoursesResponse> GetMyLearningAsync(GetMyLearningCoursesQuery request, CancellationToken ct = default);

		Task<UpsertCourseRatingResponse> UpsertCourseRatingAsync(Guid courseId, short rating, CancellationToken ct = default);
	}
}
