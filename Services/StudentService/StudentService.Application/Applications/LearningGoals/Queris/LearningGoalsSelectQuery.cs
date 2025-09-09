using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningGoals.Queris;

public record LearningGoalsSelectQuery() : IQuery<LearningGoalsSelectResponse>;