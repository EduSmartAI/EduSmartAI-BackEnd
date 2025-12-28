namespace Course.Application.LessonNotes.Commands.UpdateNote
{
	public record UpdateNoteCommand(Guid NoteId, string Content) : ICommand<UpdateNoteResponse>;

	public sealed record UpdateNoteResponse : AbstractApiResponse<bool> 
	{ 
		public override bool Response { get; set; } = default!;
	}
}
