using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath
{
    public class UpdateStatusLearningPathHandler(ILearningPathService _learningPathService) : ICommandHandler<UpdateStatusLearningPathCommand, UpdateStatusLearningPathResponse>
    {
        public async Task<UpdateStatusLearningPathResponse> Handle(UpdateStatusLearningPathCommand request, CancellationToken cancellationToken)
        {
            var response = await _learningPathService.UpdateStatusLearningPathByIdAndSortPosition(request, cancellationToken);
            return response;
        }
    }
}
