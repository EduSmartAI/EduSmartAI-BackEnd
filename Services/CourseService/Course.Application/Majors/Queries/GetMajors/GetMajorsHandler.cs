namespace Course.Application.Majors.Queries.GetMajors
{
	public class GetMajorsHandler(IMajorService _majorService) : IQueryHandler<GetMajorsQuery, GetMajorsResponse>
	{
		public async Task<GetMajorsResponse> Handle(GetMajorsQuery request, CancellationToken cancellationToken)
		{
			return await _majorService.GetMajorsAsync(
				request.Page,
				request.Size,
				request.Search,
				cancellationToken
			);
		}
	}
}
