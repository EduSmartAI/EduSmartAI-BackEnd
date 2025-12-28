using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Students.Queries;

public record StudentTranscriptSelectResponse : AbstractApiResponse<List<StudentTranscriptSelectResponseEntity>>
{
    public override List<StudentTranscriptSelectResponseEntity> Response { get; set; }
}

public class StudentTranscriptSelectResponseEntity
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

    public DateTime CreatedAt { get; set; }
}