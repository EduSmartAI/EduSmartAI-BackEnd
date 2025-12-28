namespace Course.Application.LessonNotes.Commands.DeleteNote
{
	public record DeleteNoteCommand(Guid NoteId) : ICommand<DeleteNoteResponse>;

	public sealed record DeleteNoteResponse : AbstractApiResponse<bool> 
	{ 
		public override bool Response { get; set; } = default!;
	}
}
