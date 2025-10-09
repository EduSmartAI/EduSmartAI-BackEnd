using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands.InsertInternal
{
    public class InsertInternalLearningPathCommand : ICommand<InsertInternalLearningPathResponse>
    {
        public Guid PathId { get; set; }
        public int MajorType { get; set; }
        public List<InternCourse> Courses { get; set; }
    }
    public record InternCourse(Guid courseId);
}
