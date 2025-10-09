using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.InsertInternal
{
    public class InsertInternalLearningPathHandler(ILearningPathService _learningPathService) : ICommandHandler<InsertInternalLearningPathCommand, InsertInternalLearningPathResponse>
    {
        public async Task<InsertInternalLearningPathResponse> Handle(InsertInternalLearningPathCommand request, CancellationToken cancellationToken)
        {
            var response = await _learningPathService.InsertInternalMajorAndCourse(request, cancellationToken);
            return response;
        }
    }
}
