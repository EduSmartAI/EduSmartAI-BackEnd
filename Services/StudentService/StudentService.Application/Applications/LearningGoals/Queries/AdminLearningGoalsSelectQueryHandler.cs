using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningGoals.Queries;

public class AdminLearningGoalsSelectQueryHandler(ILearningGoalService learningGoalService) : IQueryHandler<AdminLearningGoalsSelectQuery, AdminLearningGoalsSelectResponse>
{
    public async Task<AdminLearningGoalsSelectResponse> Handle(AdminLearningGoalsSelectQuery request, CancellationToken cancellationToken)
    {
        return await learningGoalService.SelectAdminLearningGoalsAsync(request);
    }
}


