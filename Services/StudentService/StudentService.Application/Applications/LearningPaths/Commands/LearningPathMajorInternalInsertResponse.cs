using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.QuizService;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public record LearningPathMajorInternalInsertResponse : AbstractApiResponse<Guid>
{
    public override Guid Response { get; set; }
    public List<Guid>? InsertedMajorIds { get; set; }
    public Guid StudentMajorId { get; set; }
    
    public List<StudentCurriculumEvent> StudentCurriculums { get; set; }

}