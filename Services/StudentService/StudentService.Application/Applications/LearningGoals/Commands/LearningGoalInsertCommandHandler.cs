using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Commands;

public class LearningGoalInsertCommandHandler : ICommandHandler<LearningGoalInsertCommand, LearningGoalInsertResponse>
{
    private readonly ILearningGoalService _learningGoalService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="learningGoalService"></param>
    public LearningGoalInsertCommandHandler(ILearningGoalService learningGoalService)
    {
        _learningGoalService = learningGoalService;
    }

    /// <summary>
    /// Handle insert learning goal
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public async Task<LearningGoalInsertResponse> Handle(LearningGoalInsertCommand request, CancellationToken cancellationToken)
    {
        return await _learningGoalService.InsertLearningGoalAsync(request, cancellationToken);
    }
}