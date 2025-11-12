namespace Course.Application.LessonNotes.Queries.GetLessonNotes
{
	public class GetLessonNotesQueryHandler(ILessonNoteService _lessonNoteService) : IQueryHandler<GetLessonNotesQuery, GetLessonNotesResponse>
	{
		public async Task<GetLessonNotesResponse> Handle(GetLessonNotesQuery request, CancellationToken cancellationToken)
		=> await _lessonNoteService.GetByLessonAsync(request.LessonId, request.Page, request.Size, cancellationToken);
	}
}
