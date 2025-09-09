using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Queris;

public class LearningGoalsSelectQueryHandler : IQueryHandler<LearningGoalsSelectQuery, LearningGoalsSelectResponse>
{
    private readonly ILearningGoalService _learningGoalService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="learningGoalService"></param>
    public LearningGoalsSelectQueryHandler(ILearningGoalService learningGoalService)
    {
        _learningGoalService = learningGoalService;
    }

    /// <summary>
    /// Handle select learning goals query
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningGoalsSelectResponse> Handle(LearningGoalsSelectQuery request, CancellationToken cancellationToken)
    {
        return await _learningGoalService.SelectLearningGoalsAsync(request);
    }
}