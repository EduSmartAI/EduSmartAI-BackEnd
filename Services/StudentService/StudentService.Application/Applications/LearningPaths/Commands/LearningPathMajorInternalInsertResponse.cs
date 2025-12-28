using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.QuizService;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public record LearningPathMajorInternalInsertResponse : AbstractApiResponse<Guid>
{
    public override Guid Response { get; set; }
    
    public Guid StudentMajorId { get; set; }
    
    public List<MajorInternalInsertResponse> Majors { get; set; } = null!;

    public List<StudentCurriculumEvent> StudentCurriculums { get; set; } = null!;
}

public class MajorInternalInsertResponse
{
    public required Guid LearningPathMajorId { get; set; }
    
    public required string MajorCode { get; set; }
    
    public required string MajorName { get; set; }
}