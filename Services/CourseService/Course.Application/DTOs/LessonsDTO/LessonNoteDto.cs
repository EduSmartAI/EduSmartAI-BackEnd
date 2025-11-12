namespace Course.Application.DTOs.LessonsDTO
{
	public sealed record LessonNoteDto(Guid NoteId, Guid LessonId, int TimeSeconds, string Content, DateTimeOffset CreatedAt);
}
