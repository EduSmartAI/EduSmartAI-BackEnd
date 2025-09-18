using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;

namespace StudentService.Application.Applications.LearningGoals.Queries;

public record LearningGoalSelectsQuery() : IQuery<LearningGoalSelectsEventResponse>;