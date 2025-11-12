using BaseService.Application.Common;
using Course.Application.DTOs.LessonsDTO;

namespace Course.Application.LessonNotes.Queries.GetLessonNotes
{
	public record GetLessonNotesQuery(Guid LessonId, int? Page, int? Size) : IQuery<GetLessonNotesResponse>;

	public sealed record GetLessonNotesResponse : AbstractApiResponse<PagedResult<LessonNoteDto>> 
	{ 
		public override PagedResult<LessonNoteDto> Response { get; set; } = default!; 
	}
}
