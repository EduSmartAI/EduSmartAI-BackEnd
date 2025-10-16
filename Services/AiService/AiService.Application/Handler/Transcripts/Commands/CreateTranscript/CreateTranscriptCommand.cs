using AiService.Application.DTOs;
using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace AiService.Application.Handler.Transcripts.Commands.CreateTranscript
{
	public record CreateTranscriptCommand(CreateTranscriptionReq CreateTranscriptionReq) : ICommand<CreateTranscriptResponse>;

	public record CreateTranscriptResponse : AbstractApiResponse<TranscriptResult>
	{
		public override TranscriptResult Response { get ; set ; }
	}
}
