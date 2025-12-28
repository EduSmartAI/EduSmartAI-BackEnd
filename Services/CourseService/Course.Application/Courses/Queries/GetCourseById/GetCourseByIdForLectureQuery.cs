using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.GetCourseById
{
	public record GetCourseByIdForLectureQuery(Guid Id) : IQuery<GetCourseByIdForLectureResponse>;

	public record GetCourseByIdForLectureResponse : AbstractApiResponse<CourseDetailForLectureDto>
	{
		public override CourseDetailForLectureDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
