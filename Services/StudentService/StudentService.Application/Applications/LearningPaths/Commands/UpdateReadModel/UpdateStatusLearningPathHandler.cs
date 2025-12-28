using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel
{
    public class UpdateReadModelLearningPathHandler(ILearningPathService _learningPathService) : ICommandHandler<UpdateReadModelLearningPathCommand, UpdateReadModelLearningPathResponse>
    {
        public async Task<UpdateReadModelLearningPathResponse> Handle(UpdateReadModelLearningPathCommand request, CancellationToken cancellationToken)
        {
            var response = await _learningPathService.UpdateStatusLearningPathReadModelByIdAndSortPosition(request, cancellationToken);
            return response;
        }
    }
}
