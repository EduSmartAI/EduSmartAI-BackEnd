namespace Course.Application.LessonNotes.Commands.UpdateNote
{
	public class UpdateNoteCommandHandler(ILessonNoteService _lessonNoteService) : ICommandHandler<UpdateNoteCommand, UpdateNoteResponse>
	{
		public async Task<UpdateNoteResponse> Handle(UpdateNoteCommand request, CancellationToken cancellationToken)
		=> await _lessonNoteService.UpdateAsync(request.NoteId, request.Content, cancellationToken);
	}
}
