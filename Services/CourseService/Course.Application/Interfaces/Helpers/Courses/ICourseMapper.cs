using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;
using Course.Application.DTOs.LessonsDTO.LessonStudentDTO;
using Course.Application.DTOs.ModulesDTO.ModuleStudentDTO;
using Course.Application.DTOs.QuizDTO;

namespace Course.Application.Interfaces.Helpers.Courses
{
	public interface ICourseMapper
	{
		CourseDto ToDto(CourseEntity e);

		CourseDetailForGuestDto MapCourseDetailForGuest(CourseEntity e);

		CourseDetailForLectureDto MapCourseDetailForLecture(
			CourseEntity e,
			IReadOnlyDictionary<Guid, Guid>? moduleQuizIdByModuleId,     // moduleId -> quizId
			IReadOnlyDictionary<Guid, Guid>? lessonQuizIdByLessonId,     // lessonId -> quizId
			IReadOnlyDictionary<Guid, QuizOutDto?>? quizByQuizId         // quizId -> QuizOutDto
		);

		CourseDetailForStudentDto MapCourseDetailForStudent(
			CourseEntity e,
			IReadOnlyDictionary<Guid, Guid>? moduleQuizIdByModuleId,
			IReadOnlyDictionary<Guid, Guid>? lessonQuizIdByLessonId,
			IReadOnlyDictionary<Guid, QuizOutDto?>? quizByQuizId,
			IReadOnlyDictionary<Guid, LessonProgressSnap> progressByLessonId,   // lessonId -> { Status, LastPositionSec, CompletedAt }
			IReadOnlyDictionary<Guid, ModuleProgressSnap> moduleProgressById,   // moduleId -> { LessonsTotal, LessonsCompleted, PercentCompleted, Status, StartedAt, CompletedAt }
			CourseProgressSnap? courseProgress,                                 // { LessonsTotal, LessonsCompleted, PercentCompleted, Status, StartedAt, CompletedAt }
			bool preferCoreForCourse
		);
	}
}
