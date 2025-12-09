using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService;

public sealed record GetSubjectDetailEvent
{
    public string SubjectCode { get; init; } = string.Empty;
}

public sealed record GetSubjectDetailEventResponse : AbstractApiResponse<SubjectDetailDto>
{
    public override SubjectDetailDto Response { get; set; } = new();
}

public sealed record SubjectDetailDto
{
    public string SubjectCode { get; init; } = string.Empty;
    public string SubjectTitle { get; init; } = string.Empty;
    public string SubjectDescription { get; init; } = string.Empty;
}






