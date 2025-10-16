using AiService.Application.Contracts;
using AiService.Application.Interfaces;
using BuildingBlocks.CQRS;

namespace AiService.Application.Handler.Transcripts.Commands.CreateTranscript
{
	public class CreateTranscriptHandler(ITranscriptionService _transcriptionService) : ICommandHandler<CreateTranscriptCommand, CreateTranscriptResponse>
	{
		public async Task<CreateTranscriptResponse> Handle(CreateTranscriptCommand request, CancellationToken cancellationToken)
		{
			// Enqueue transcription job
			var transcribeJob = new TranscribeJob(
				request.CreateTranscriptionReq.LessonId ?? string.Empty,
				request.CreateTranscriptionReq.VideoUrl!,
				request.CreateTranscriptionReq.Language ?? "vi",
				request.CreateTranscriptionReq.DurationSec);

			return await _transcriptionService.ProcessAsync(transcribeJob, cancellationToken);
		}
	}
}
