using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;

namespace Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseIdForStudents
{
	public record GetDetailsProgressByCourseIdForStudentQuery(Guid Id) : IQuery<GetDetailsProgressByCourseIdForStudentResponse>;

	public record GetDetailsProgressByCourseIdForStudentResponse : AbstractApiResponse<CourseDetailForStudentDto>
	{
		public override CourseDetailForStudentDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
