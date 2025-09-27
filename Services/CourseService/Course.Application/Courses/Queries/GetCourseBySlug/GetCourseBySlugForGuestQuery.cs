using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.GetCourseBySlug
{
	public record GetCourseBySlugForGuestQuery(string Slug) : IQuery<GetCourseBySlugForGuestResponse>;

	public record GetCourseBySlugForGuestResponse : AbstractApiResponse<CourseDetailForGuestDto>
	{
		public override CourseDetailForGuestDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
