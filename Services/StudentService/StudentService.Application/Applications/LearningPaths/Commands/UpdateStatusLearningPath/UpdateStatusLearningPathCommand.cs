using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath
{
    public class UpdateStatusLearningPathCommand : ICommand<UpdateStatusLearningPathResponse>
    {
        public Guid LearningPathId { get; set; }
        public List<Guid> InternalMajorIds { get; set; } = [];
    }
}
