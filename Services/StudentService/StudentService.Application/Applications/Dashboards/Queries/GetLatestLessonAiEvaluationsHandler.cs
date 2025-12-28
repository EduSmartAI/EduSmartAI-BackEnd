using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public class GetLatestLessonAiEvaluationsHandler(IAiQuizEvaluateStudentService _aiQuizEvaluateStudentService) : IQueryHandler<GetLatestLessonAiEvaluationsQuery, GetLatestLessonAiEvaluationsResponse>
	{
		public async Task<GetLatestLessonAiEvaluationsResponse> Handle(GetLatestLessonAiEvaluationsQuery request, CancellationToken cancellationToken)
		{
			return await _aiQuizEvaluateStudentService.GetLatestLessonAiEvaluationsAsync(request, cancellationToken);
		}
	}
}
