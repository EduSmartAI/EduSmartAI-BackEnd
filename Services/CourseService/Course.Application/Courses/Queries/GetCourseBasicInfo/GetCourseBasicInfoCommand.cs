using BuildingBlocks.Messaging.Events.StudentService;

namespace Course.Application.Courses.Queries.GetCourseBasicInfo
{
	public record GetCourseBasicInfoCommand(List<Guid> CourseIds) : ICommand<GetCourseBasicInfoResponse>;
}
