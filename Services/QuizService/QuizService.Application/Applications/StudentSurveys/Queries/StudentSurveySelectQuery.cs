using BuildingBlocks.CQRS;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public record StudentSurveySelectQuery() : IQuery<StudentSurveySelectResponse>;