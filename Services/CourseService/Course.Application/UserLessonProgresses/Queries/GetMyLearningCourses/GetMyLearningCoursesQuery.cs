using BuildingBlocks.Pagination;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.UserLessonProgresses.Queries.GetMyLearningCourses
{
	public sealed record GetMyLearningCoursesQuery(int? Page = 1, int? Size = 10, string? Search = null, bool NoCache = false) : IQuery<GetMyLearningCoursesResponse>;

	public sealed record GetMyLearningCoursesResponse : AbstractApiResponse<PaginatedResult<MyLearningCourseItemDto>>
	{
		public override PaginatedResult<MyLearningCourseItemDto> Response { get; set; } = default!;
	}
}
