using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public class LearningGoalDeleteCommandHandler(ILearningGoalService learningGoalService) : ICommandHandler<LearningGoalDeleteCommand, LearningGoalDeleteResponse>
{
    public async Task<LearningGoalDeleteResponse> Handle(LearningGoalDeleteCommand request, CancellationToken cancellationToken)
    {
        return await learningGoalService.DeleteLearningGoalAsync(request, cancellationToken);
    }
}


