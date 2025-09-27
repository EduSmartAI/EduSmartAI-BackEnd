using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;

namespace Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseSlugForStudents
{
	public record GetDetailsProgressByCourseSlugForStudentQuery(string Slug) : IQuery<GetDetailsProgressByCourseSlugForStudentResponse>;

	public record GetDetailsProgressByCourseSlugForStudentResponse : AbstractApiResponse<CourseDetailForStudentDto>
	{
		public override CourseDetailForStudentDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
