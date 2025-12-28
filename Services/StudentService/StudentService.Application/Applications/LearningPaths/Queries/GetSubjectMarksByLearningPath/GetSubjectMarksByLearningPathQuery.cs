using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Queries.GetSubjectMarksByLearningPath;

namespace StudentService.Application.Applications.LearningPaths.Queries.GetSubjectMarksByLearningPath
{
    public record GetSubjectMarksByLearningPathQuery : IQuery<GetSubjectMarksByLearningPathResponse>
    {
        public Guid LearningPathId { get; set; }
    }
}

