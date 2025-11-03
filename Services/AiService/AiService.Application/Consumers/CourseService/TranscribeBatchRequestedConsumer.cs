using AiService.Application.Contracts;
using AiService.Application.Interfaces;
using BuildingBlocks.Messaging.Events.CourseService.AITranscriptEvents;
using MassTransit;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace AiService.Application.Consumers.CourseService
{
	public class TranscribeBatchRequestedConsumer(ITranscriptionService _transcriptionService, IPublishEndpoint _publish) : IConsumer<TranscribeBatchRequested>
	{
		public async Task Consume(ConsumeContext<TranscribeBatchRequested> ctx)
		{
			var msg = ctx.Message;
			foreach (var item in msg.Items)
			{
				var job = new TranscribeJob(
					LessonId: item.LessonId.ToString(),
					VideoUrl: item.VideoUrl,
					Language: msg.Language,
					DurationSec: item.DurationSec
				);

				//await _transcriptionService.ProcessAsync(job, ctx.CancellationToken);

				var res = await _transcriptionService.ProcessAsync(job, ctx.CancellationToken);

				var ok = res.Success && res.Response is not null;
				var r = res.Response;

				var upsert = new TranscriptUpsertEvent(
					CorrelationId: msg.CorrelationId,
					LessonId: item.LessonId,
					Language: msg.Language,
					Status: ok ? (short)TranscriptStatus.Succeeded : (short)TranscriptStatus.Failed,
					TextFull: ok ? r!.Text : null,
					VttUrl: ok ? r!.VttUrl : null,
					VttPublicId: ok ? r!.VttPublicId : null,
					Error: ok ? null : res.Message,
					ProcessedAtUtc: DateTime.UtcNow
				);

				await _publish.Publish(upsert, ctx.CancellationToken);
			}
		}
	}
}
