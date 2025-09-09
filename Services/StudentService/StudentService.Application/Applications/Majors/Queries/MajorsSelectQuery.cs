using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Majors.Queries;

public record MajorsSelectQuery() : IQuery<MajorsSelectResponse>;