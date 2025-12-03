using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.QuizService;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public record LearningPathMajorInternalInsertResponse : AbstractApiResponse<Guid>
{
    public override Guid Response { get; set; }
    public List<MajorInternalInsertResponse>? Majors { get; set; }
    public Guid StudentMajorId { get; set; }
    
    public List<StudentCurriculumEvent> StudentCurriculums { get; set; }

}

public class MajorInternalInsertResponse
{
    public Guid MajorId { get; set; }

    public string MajorCode { get; set; }

    public string MajorName { get; set; }
}