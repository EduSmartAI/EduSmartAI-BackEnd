using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.QuizzesCourseInsertEvents
{
	public record ModuleLessonQuizInsertEventResponse : AbstractApiResponse<ModuleLessonQuizInsertEventResponseEntity>
	{
		public override ModuleLessonQuizInsertEventResponseEntity Response { get; set; }
	}


	public record ModuleLessonQuizInsertEventResponseEntity(
		Guid CorrelationId,
		Guid QuizId,
		Guid? ModuleId,
		Guid? LessonId
	);

}
