using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Students.Queries;

public record StudentTechnologyGoalSelectQuery : IQuery<StudentTechnologyGoalSelectResponse>
{
}

