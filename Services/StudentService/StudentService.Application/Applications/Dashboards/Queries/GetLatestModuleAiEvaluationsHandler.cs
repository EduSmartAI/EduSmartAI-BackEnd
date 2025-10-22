using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public class GetLatestModuleAiEvaluationsHandler(IAiQuizEvaluateStudentService _aiQuizEvaluateStudentService) : IQueryHandler<GetLatestModuleAiEvaluationsQuery, GetLatestModuleAiEvaluationsResponse>
	{
		public async Task<GetLatestModuleAiEvaluationsResponse> Handle(GetLatestModuleAiEvaluationsQuery request, CancellationToken cancellationToken)
		{
			return await _aiQuizEvaluateStudentService.GetLatestModuleAiEvaluationsAsync(request, cancellationToken);
		}
	}
}
