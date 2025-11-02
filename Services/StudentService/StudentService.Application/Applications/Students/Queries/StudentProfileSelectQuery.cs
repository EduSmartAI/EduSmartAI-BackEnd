using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Students.Queries;

public record StudentProfileSelectQuery : IQuery<StudentProfileSelectResponse>
{
}