namespace Course.Application.LessonNotes.Commands.CreateNote
{
	public record CreateNoteCommand(Guid LessonId, int TimeSeconds, string Content) : ICommand<CreateNoteResponse>;

	public sealed record CreateNoteResponse : AbstractApiResponse<bool> 
	{ 
		public override bool Response { get; set; } = default!; 
	}
}
