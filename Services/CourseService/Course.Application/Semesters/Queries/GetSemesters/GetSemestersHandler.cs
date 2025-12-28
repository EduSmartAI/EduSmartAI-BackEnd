namespace Course.Application.Semesters.Queries.GetSemesters
{
	public class GetSemestersHandler(ISemesterService _semesterService) : IQueryHandler<GetSemestersQuery, GetSemestersResponse>
	{
		public async Task<GetSemestersResponse> Handle(GetSemestersQuery request, CancellationToken cancellationToken)
		{
			return await _semesterService.GetSemestersAsync(
				request.Page,
				request.Size,
				request.Search,
				cancellationToken
			);
		}
	}
}
