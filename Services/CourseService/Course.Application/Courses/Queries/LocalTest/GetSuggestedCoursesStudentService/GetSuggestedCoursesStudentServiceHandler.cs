using BuildingBlocks.Messaging.Events.StudentService;

namespace Course.Application.Courses.Queries.LocalTest.GetSuggestedCoursesStudentService
{
	public class GetSuggestedCoursesStudentServiceHandler(ICourseService _courseService) : IQueryHandler<GetSuggestedCoursesStudentServiceQuery, GetSuggestedCoursesEventResponse>
	{
		public async Task<GetSuggestedCoursesEventResponse> Handle(GetSuggestedCoursesStudentServiceQuery request, CancellationToken cancellationToken)
		{
			return await _courseService.GetSuggestedCoursesAsync(request.GetSuggestedCoursesStudentServiceDto, cancellationToken);
		}
	}
}
