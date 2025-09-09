using BuildingBlocks.CQRS;

namespace StudentService.Application.Majors.Queries;

public record MajorsSelectQuery() : IQuery<MajorsSelectResponse>;