using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.Quizzes.Queries;

public record QuizSelectsQuery() : IQuery<QuizSelectsResponse>;