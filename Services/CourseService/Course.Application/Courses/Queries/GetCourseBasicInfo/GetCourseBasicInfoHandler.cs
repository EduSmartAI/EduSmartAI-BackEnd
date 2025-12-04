using BuildingBlocks.Messaging.Events.StudentService;

namespace Course.Application.Courses.Queries.GetCourseBasicInfo
{
	public class GetCourseBasicInfoHandler(ICourseService _courseInfoService) : ICommandHandler<GetCourseBasicInfoCommand, GetCourseBasicInfoResponse>
	{
		public async Task<GetCourseBasicInfoResponse> Handle(GetCourseBasicInfoCommand request, CancellationToken cancellationToken)
		{
			return await _courseInfoService.GetBasicCoursesInforAsync(request.CourseIds, cancellationToken);
		}
	}
}
