using BuildingBlocks.CQRS;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;

namespace StudentService.Application.Applications.LearningPaths.Queries
{
    public record LearningPathSelectsQuery() : IQuery<LearningPathSelectResponse>
    {
        public Guid LearningPathId { get; set; }
    }
}
