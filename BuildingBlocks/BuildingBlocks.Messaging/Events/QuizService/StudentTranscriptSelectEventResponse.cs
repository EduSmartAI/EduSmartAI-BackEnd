using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.QuizService;

public record StudentTranscriptSelectEventResponse : AbstractApiResponse<List<StudentTranscriptSelectEventResponseEntity>>
{
    public override List<StudentTranscriptSelectEventResponseEntity> Response { get; set; }
}

public class StudentTranscriptSelectEventResponseEntity
{
    public Guid StudentTranscriptId { get; set; }
    
    public string Semester { get; set; } = null!;

    public int SemesterNumber { get; set; }

    public string SubjectCode { get; set; } = null!;

    public string? Prerequisite { get; set; }

    public string SubjectName { get; set; } = null!;

    public int Credit { get; set; }

    public double? Grade { get; set; }

    public string Status { get; set; } = null!;
}