using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;

public record LearningGoalSelectsEventResponse : AbstractApiResponse<List<LearningGoalSelectsEventResponseEntity>>
{
    public override List<LearningGoalSelectsEventResponseEntity> Response { get; set; }
}

public record LearningGoalSelectsEventResponseEntity
{
    public Guid LearningGoalId { get; set; }
    
    public string LearningGoalName { get; set; }
    
    public short LearningGoalType { get; set; }
}