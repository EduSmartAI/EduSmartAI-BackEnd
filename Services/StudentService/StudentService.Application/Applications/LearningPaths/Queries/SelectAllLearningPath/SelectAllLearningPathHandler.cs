using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath
{
    public class SelectAllLearningPathHandler(ILearningPathService _learningPathService) : IQueryHandler<SelectAllLearningPathQuery, SelectAllLearningPathResponse>
    {
        public async Task<SelectAllLearningPathResponse> Handle(SelectAllLearningPathQuery request, CancellationToken cancellationToken)
        {
            var response = await _learningPathService.GetAllLearningPath(request);
            return response;
        }
    }
}
