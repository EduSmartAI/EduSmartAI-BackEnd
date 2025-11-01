using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public class LearningGoalUpdateCommandHandler(ILearningGoalService learningGoalService) : ICommandHandler<LearningGoalUpdateCommand, LearningGoalUpdateResponse>
{
    public async Task<LearningGoalUpdateResponse> Handle(LearningGoalUpdateCommand request, CancellationToken cancellationToken)
    {
        return await learningGoalService.UpdateLearningGoalAsync(request, cancellationToken);
    }
}