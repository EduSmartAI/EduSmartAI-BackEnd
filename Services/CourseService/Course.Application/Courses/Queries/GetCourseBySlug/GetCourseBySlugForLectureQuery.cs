using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.GetCourseBySlug
{
	public record GetCourseBySlugForLectureQuery(string Slug) : IQuery<GetCourseBySlugForLectureResponse>;

	public record GetCourseBySlugForLectureResponse : AbstractApiResponse<CourseDetailForLectureDto>
	{
		public override CourseDetailForLectureDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
