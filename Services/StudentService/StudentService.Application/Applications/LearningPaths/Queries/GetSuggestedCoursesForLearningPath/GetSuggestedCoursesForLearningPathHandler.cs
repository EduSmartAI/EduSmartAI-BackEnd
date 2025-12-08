using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Queries.GetSuggestedCoursesForLearningPath
{
	public class GetSuggestedCoursesForLearningPathHandler(ILearningPathService _learningPathService) : IQueryHandler<GetSuggestedCoursesForLearningPathQuery, GetSuggestedCoursesForLearningPathResponse>
	{
		public async Task<GetSuggestedCoursesForLearningPathResponse> Handle(GetSuggestedCoursesForLearningPathQuery request, CancellationToken cancellationToken)
		{
			return await _learningPathService.GetSuggestedCoursesForLearningPathAsync(request, cancellationToken);
		}
	}
}
