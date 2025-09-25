using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;

namespace Course.Application.Courses.Queries.GetCourseById
{
	public record GetCourseByIdForStudentQuery(Guid Id) : IQuery<GetCourseByIdForStudentResponse>;

	public record GetCourseByIdForStudentResponse : AbstractApiResponse<CourseDetailForStudentDto>
	{
		public override CourseDetailForStudentDto Response { get; set; } = default!;
		public int ModulesCount { get; set; }
		public int LessonsCount { get; set; }
	}
}
