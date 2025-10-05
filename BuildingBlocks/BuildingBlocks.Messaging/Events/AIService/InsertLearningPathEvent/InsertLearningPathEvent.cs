using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent
{
    public sealed record InsertLearningPathEvent
    {
        public Guid LearningPathId { get; init; }
        public string PathName { get; init; } = null!;
        public Guid StudentId { get; init; }
        public string CurrentUserEmail { get; init; } = null!;
        
        public List<InsertLearningPathMajor> Majors { get; init; } = null!;
    }

    public class InsertLearningPathMajor
    {
        public string MajorCode { get; init; } = null!;
        
        public string Reason { get; init; } = null!;
        
        public short Type { get; set; }
        
        public List<InsertLearningPathCourse> Courses { get; init; } = null!;
    }
    
    public class InsertLearningPathCourse
    {
        public Guid? InternalCourseId { get; set; }

        public int? Position { get; set; }
        
        public string? StepName { get; set; }

        public string? ExternalCourseLink { get; set; }

        public string? ExternalCourseReason { get; set; }

        public decimal? ExternalCourseRating { get; set; }

        public string? ExternalCourseLevel { get; set; }

        public string? ExternalCourseDuration { get; set; }

        public string? ExternalCourseProvider { get; set; }

    }

    public record InsertLearningPathEventResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
    
}
