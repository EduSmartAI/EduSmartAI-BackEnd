using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queris;

namespace StudentService.Application.Interfaces;

public interface ILearningGoalService
{
    Task<LearningGoalInsertResponse> InsertLearningGoalAsync(LearningGoalInsertCommand request, CancellationToken cancellationToken);
    
    Task<LearningGoalsSelectResponse> SelectLearningGoalsAsync(LearningGoalsSelectQuery request);
}