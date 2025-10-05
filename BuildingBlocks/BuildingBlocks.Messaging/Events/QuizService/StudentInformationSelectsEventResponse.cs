using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record StudentInformationSelectsEventResponse : AbstractApiResponse<StudentInformationSelectsEventResponseEntity>
{
    public override StudentInformationSelectsEventResponseEntity Response { get; set; }
}

public class StudentInformationSelectsEventResponseEntity
{
    public Guid SemesterId { get; set; }
    public string LearningGoalName { get; set; }
    
    public short LearningGoalType { get; set; }
    
    public List<StudentTechnologySelectsEventResponseEntity> Technologies { get; set; }
}

public class StudentTechnologySelectsEventResponseEntity
{
    public string TechnologyName { get; set; }
    
    public short TechnologyType { get; set; }   
}