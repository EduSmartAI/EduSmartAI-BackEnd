using BuildingBlocks.Messaging.Events.CourseService.AITranscriptEvents;
using Course.Application.Courses.Commands.CreateLessonTranscript;

namespace Course.Application.Courses.Consumers
{
	public class TranscriptUpsertConsumer(ILessonService lessonService) : IConsumer<TranscriptUpsertEvent>
	{
		public async Task Consume(ConsumeContext<TranscriptUpsertEvent> context)
		{
			var message = context.Message;
			var command = new CreateLessonTranscriptCommand(
				message.LessonId,
				message.Language,
				message.Status,
				message.TextFull,
				message.VttUrl,
				message.VttPublicId,
				message.Error
			);

			await lessonService.CreateAsync(command, context.CancellationToken);
		}
	}
}
