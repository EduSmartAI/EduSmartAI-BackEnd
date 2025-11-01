using BaseService.Application.Common;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Queries;

public class AdminLearningGoalsSelectQuery : IQuery<AdminLearningGoalsSelectResponse>
{
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
    
    public string? SearchTerm { get; set; }
    
    public short? LearningGoalType { get; set; }
}