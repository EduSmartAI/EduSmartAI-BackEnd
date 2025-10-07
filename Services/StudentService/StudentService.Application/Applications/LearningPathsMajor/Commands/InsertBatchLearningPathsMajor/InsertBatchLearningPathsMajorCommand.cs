using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;

public class InsertBatchLearningPathsMajorCommand : ICommand<InsertBatchLearningPathsMajorResponse>
{
    public Guid PathId { get; set; }
    public string CurrentUserEmail { get; set; } = null!;
    public List<ExternalMajorBatchItem> Majors { get; set; } = new();
}

public class ExternalMajorBatchItem
{
    public string MajorCode { get; set; } = null!;
    public string? Reason { get; set; }
    public List<StepExternalMajorBatchItem>? Steps { get; set; }
}

public class StepExternalMajorBatchItem
{
    public int Order { get; set; }
    public string Title { get; set; } = null!;
    public int DurationWeeks { get; set; }
    public List<string> Objectives { get; set; } = new();
    public List<StepCourseBatchItem> SuggestedCourses { get; set; } = new();
}

public class StepCourseBatchItem
{
    public string Title { get; set; } = null!;
    public string Link { get; set; } = null!;
    public string Provider { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string Duration { get; set; } = null!;
    public string Level { get; set; } = null!;
}

