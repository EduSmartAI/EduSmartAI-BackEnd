using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents
{
	public record QuizCourseCheckAttemptEventResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}
}
