using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;

public class InsertBatchLearningPathsMajorCommandHandler(ILearningPathService learningPathService) : ICommandHandler<InsertBatchLearningPathsMajorCommand, InsertBatchLearningPathsMajorResponse>
{
    public async Task<InsertBatchLearningPathsMajorResponse> Handle(InsertBatchLearningPathsMajorCommand request, CancellationToken cancellationToken)
    {
        return await learningPathService.InsertBatchLearningPathMajorCourseAsync(request, cancellationToken);
    }
}

