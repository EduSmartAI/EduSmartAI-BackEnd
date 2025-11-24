namespace Course.Application.Syllabus.Queries
{
	public class GetFullSyllabusHandler(ISyllabusService service)
	: IQueryHandler<GetFullSyllabusQuery, GetFullSyllabusResponse>
	{
		public async Task<GetFullSyllabusResponse> Handle(GetFullSyllabusQuery request, CancellationToken ct)
			=> await service.GetFullSyllabusAsync(request.VersionLabel, ct);
	}
}
