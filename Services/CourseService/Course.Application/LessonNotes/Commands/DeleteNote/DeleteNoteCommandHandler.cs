namespace Course.Application.LessonNotes.Commands.DeleteNote
{
	public class DeleteNoteCommandHandler(ILessonNoteService _lessonNoteService) : ICommandHandler<DeleteNoteCommand, DeleteNoteResponse>
	{
		public async Task<DeleteNoteResponse> Handle(DeleteNoteCommand request, CancellationToken cancellationToken)
		=> await _lessonNoteService.DeleteAsync(request.NoteId, cancellationToken);
	}
}