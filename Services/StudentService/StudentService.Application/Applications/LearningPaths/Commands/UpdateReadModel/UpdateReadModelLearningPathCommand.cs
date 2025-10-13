using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel
{
    public class UpdateReadModelLearningPathCommand : ICommand<UpdateReadModelLearningPathResponse>
    {
        public Guid LearningPathId { get; set; }
    }
}
