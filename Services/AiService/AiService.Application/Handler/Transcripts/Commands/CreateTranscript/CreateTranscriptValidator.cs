using FluentValidation;

namespace AiService.Application.Handler.Transcripts.Commands.CreateTranscript
{
	public class CreateTranscriptValidator : AbstractValidator<CreateTranscriptCommand>
	{
		public CreateTranscriptValidator()
		{
			RuleFor(x => x.CreateTranscriptionReq.LessonId).NotEmpty().WithMessage("LessonId is required.");
			RuleFor(x => x.CreateTranscriptionReq.VideoUrl).NotEmpty().WithMessage("VideoUrl is required.");
		}
	}
}
