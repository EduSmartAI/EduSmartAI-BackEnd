namespace Course.Application.Semesters.Queries.GetSemesterDetails
{
	public class GetSemesterDetailHandler(ISemesterService _semesterService) : IQueryHandler<GetSemesterDetailQuery, GetSemesterDetailResponse>
	{
		public async Task<GetSemesterDetailResponse> Handle(GetSemesterDetailQuery request, CancellationToken cancellationToken)
		{
			return await _semesterService.GetSemesterDetailAsync(
				request.SemesterId,
				cancellationToken
			);
		}
	}
}
