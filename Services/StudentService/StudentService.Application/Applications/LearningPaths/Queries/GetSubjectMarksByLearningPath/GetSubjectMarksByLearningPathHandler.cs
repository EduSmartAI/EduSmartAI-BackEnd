using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Queries.GetSubjectMarksByLearningPath;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Queries.GetSubjectMarksByLearningPath
{
    public class GetSubjectMarksByLearningPathHandler : IQueryHandler<GetSubjectMarksByLearningPathQuery, GetSubjectMarksByLearningPathResponse>
    {
        private readonly ILearningPathService _learningPathService;

        public GetSubjectMarksByLearningPathHandler(ILearningPathService learningPathService)
        {
            _learningPathService = learningPathService;
        }

        public async Task<GetSubjectMarksByLearningPathResponse> Handle(GetSubjectMarksByLearningPathQuery request, CancellationToken cancellationToken)
        {
            return await _learningPathService.GetSubjectMarksByLearningPathAsync(request, cancellationToken);
        }
    }
}

