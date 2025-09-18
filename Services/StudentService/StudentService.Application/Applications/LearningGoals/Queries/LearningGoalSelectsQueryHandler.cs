using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Queries;

public class LearningGoalSelectsQueryHandler : IQueryHandler<LearningGoalSelectsQuery, LearningGoalSelectsEventResponse>
{
    private readonly ILearningGoalService _learningGoalService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="learningGoalService"></param>
    public LearningGoalSelectsQueryHandler(ILearningGoalService learningGoalService)
    {
        _learningGoalService = learningGoalService;
    }

    /// <summary>
    /// Handle select learning goals query
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningGoalSelectsEventResponse> Handle(LearningGoalSelectsQuery request, CancellationToken cancellationToken)
    {
        return await _learningGoalService.SelectLearningGoalsAsync(request);
    }
}