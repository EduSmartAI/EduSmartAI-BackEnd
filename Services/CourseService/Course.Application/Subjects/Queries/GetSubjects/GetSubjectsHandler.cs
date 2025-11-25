namespace Course.Application.Subjects.Queries.GetSubjects
{
	public class GetSubjectsHandler(ISubjectService _subjectService) : IQueryHandler<GetSubjectsQuery, GetSubjectsResponse>
	{
		public async Task<GetSubjectsResponse> Handle(GetSubjectsQuery request, CancellationToken cancellationToken)
		{
			return await _subjectService.GetSubjectsAsync(
				request.Page,
				request.Size,
				request.Search,
				cancellationToken
			);
		}
	}
}
