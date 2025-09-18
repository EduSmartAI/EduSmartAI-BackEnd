using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queries;

namespace StudentService.Application.Interfaces;

public interface ILearningGoalService
{
    Task<LearningGoalInsertResponse> InsertLearningGoalAsync(LearningGoalInsertCommand request, CancellationToken cancellationToken);
    
    Task<LearningGoalSelectsEventResponse> SelectLearningGoalsAsync(LearningGoalSelectsQuery request);
}