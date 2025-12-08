using BuildingBlocks.Messaging.Events.StudentService;
using Course.Application.DTOs.CoursesDTO;

namespace Course.Application.Courses.Queries.LocalTest.GetSuggestedCoursesStudentService
{
	public record GetSuggestedCoursesStudentServiceQuery(GetSuggestedCoursesStudentServiceDto GetSuggestedCoursesStudentServiceDto) : IQuery<GetSuggestedCoursesEventResponse>;
}
