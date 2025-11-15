using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPathCourse.Commands.UpdateLearningPathCourseStatus
{
	public record UpdateLearningPathCourseStatusCommand(Guid UserId, Guid CourseId, short Status)
	: ICommand<UpdateLearningPathCourseStatusResponse>;

	public record UpdateLearningPathCourseStatusResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
