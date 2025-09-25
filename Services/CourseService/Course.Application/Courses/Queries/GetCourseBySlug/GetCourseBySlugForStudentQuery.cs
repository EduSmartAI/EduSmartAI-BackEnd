using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;

namespace Course.Application.Courses.Queries.GetCourseBySlug
{
	public record GetCourseBySlugForStudentQuery(string Slug) : IQuery<GetCourseBySlugForStudentResponse>;

	public record GetCourseBySlugForStudentResponse : AbstractApiResponse<CourseDetailForStudentDto>
	{
		public override CourseDetailForStudentDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
