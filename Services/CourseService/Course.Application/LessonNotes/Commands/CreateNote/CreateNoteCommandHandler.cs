using System.Runtime.Intrinsics.Arm;

namespace Course.Application.LessonNotes.Commands.CreateNote
{
	public class CreateNoteCommandHandler(ILessonNoteService _lessonNoteService) : ICommandHandler<CreateNoteCommand, CreateNoteResponse>
	{
		public async Task<CreateNoteResponse> Handle(CreateNoteCommand request, CancellationToken cancellationToken)
		=> await _lessonNoteService.CreateAsync(request.LessonId, request.TimeSeconds, request.Content, cancellationToken);
	}
}
