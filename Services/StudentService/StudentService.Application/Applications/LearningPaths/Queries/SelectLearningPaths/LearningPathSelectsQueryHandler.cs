using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Queries
{
    public class LearningPathSelectsQueryHandler(ILearningPathService _learningPathService) : IQueryHandler<LearningPathSelectsQuery, LearningPathSelectResponse>
    {
        public async Task<LearningPathSelectResponse> Handle(LearningPathSelectsQuery request, CancellationToken cancellationToken)
        {
            var response = await _learningPathService.GetLearningPathById(request);
            return response;
        }
    }
}
