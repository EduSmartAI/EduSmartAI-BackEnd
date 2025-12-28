using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents
{
	public record QuizCourseCheckAttemptEventResponse : AbstractApiResponse<QuizCourseCheckAttemptEntity>
	{
		public override QuizCourseCheckAttemptEntity Response { get; set; }
	}

	public class QuizCourseCheckAttemptEntity
	{
		public bool CanAttempt { get; set; }
		public Guid? StudentQuizId { get; set; }
	}
}
