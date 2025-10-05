using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public class CoursesSelectEvent{
    public List<string> MajorCodes { get; set; } = null!;

    public Guid SemesterId { get; set; }
    
    public int LimitTime { get; set; }
    public short StudentLevel { get; set; }
};

public record CoursesSelectEventResponse : AbstractApiResponse<List<CoursesSelectEventResponseEntity>>
{
    public override List<CoursesSelectEventResponseEntity> Response { get; set; }
}

public class CoursesSelectEventResponseEntity
{
    public string MajorCode { get; set; }
    
    public List<Guid> CourseCodeIds { get; set; }
}