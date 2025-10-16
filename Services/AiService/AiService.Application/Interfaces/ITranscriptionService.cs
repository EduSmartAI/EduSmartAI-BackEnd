using AiService.Application.Contracts;
using AiService.Application.Handler.Transcripts.Commands.CreateTranscript;

namespace AiService.Application.Interfaces
{
	public interface ITranscriptionService
	{
		Task<CreateTranscriptResponse> ProcessAsync(TranscribeJob job, CancellationToken ct);
	}
}
