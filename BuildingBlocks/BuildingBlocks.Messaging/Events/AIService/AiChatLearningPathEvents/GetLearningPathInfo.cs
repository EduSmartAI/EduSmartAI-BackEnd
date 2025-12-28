using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.AiChatLearningPathEvents;

public sealed record GetLearningPathInfo(Guid UserId, Guid LearningPathId);

public sealed record GetLearningPathInfoResponse : AbstractApiResponse<AiLearningPathDetailDto>
{
    public override AiLearningPathDetailDto Response { get; set; } = new();
}

public sealed record AiLearningPathDetailDto
{
    public Guid PathId { get; set; }
    public string PathName { get; set; } = string.Empty;
    public short Status { get; set; }
    public decimal CompletionPercent { get; set; }
    public List<AiLearningPathCourseGroupDto> BasicCourseGroups { get; set; } = new();
    public List<AiLearningPathInternalMajorDto> InternalMajors { get; set; } = new();
}

public sealed record AiLearningPathInternalMajorDto
{
    public string? MajorId { get; set; }
    public string? MajorCode { get; set; }
    public string? Reason { get; set; }
    public int? PositionIndex { get; set; }
    public List<AiLearningPathCourseGroupDto> CourseGroups { get; set; } = new();
}

public sealed record AiLearningPathCourseGroupDto
{
    public string SubjectCode { get; set; } = string.Empty;
    public short Status { get; set; }
    public List<AiLearningPathCourseItemDto> Courses { get; set; } = new();
}

public sealed record AiLearningPathCourseItemDto
{
    public string? CourseId { get; set; }
    public int SemesterPosition { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public short Status { get; set; }
    public string? Title { get; set; }
    public string? Provider { get; set; }
}