using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.GetInProgressCourse
{
	public record GetInProgressCourseByStudentIdQuery : IQuery<GetInProgressCourseByStudentIdResponse>;

	public record GetInProgressCourseByStudentIdResponse : AbstractApiResponse<IReadOnlyList<InProgressCourseDto>>
	{
		public override IReadOnlyList<InProgressCourseDto> Response { get; set; }
	}
}
