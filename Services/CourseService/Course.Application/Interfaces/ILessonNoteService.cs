using Course.Application.LessonNotes.Commands.CreateNote;
using Course.Application.LessonNotes.Commands.DeleteNote;
using Course.Application.LessonNotes.Commands.UpdateNote;
using Course.Application.LessonNotes.Queries.GetLessonNotes;

namespace Course.Application.Interfaces
{
	public interface ILessonNoteService
	{
		Task<CreateNoteResponse> CreateAsync(Guid lessonId, int timeSeconds, string content, CancellationToken ct = default);
		Task<UpdateNoteResponse> UpdateAsync(Guid noteId, string content, CancellationToken ct = default);
		Task<DeleteNoteResponse> DeleteAsync(Guid noteId, CancellationToken ct = default);
		Task<GetLessonNotesResponse> GetByLessonAsync(Guid lessonId, int? page, int? size, CancellationToken ct = default);
	}
}
