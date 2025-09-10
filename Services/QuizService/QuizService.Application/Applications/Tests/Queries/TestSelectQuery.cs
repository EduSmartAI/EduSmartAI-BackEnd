using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Tests.Queries;

public record TestSelectQuery() : IQuery<TestSelectResponse>;