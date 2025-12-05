using BaseService.Common.ApiEntities;
using System.Collections.Generic;

namespace BuildingBlocks.Messaging.Events.AIService;

public sealed record GetSubjectSemesterEvent
{
    public SubjectListInfo SubjectAndMajor { get; init; } = new();
}

public sealed record SubjectListInfo
{
    public string MajorCode { get; init; } = string.Empty;
    public List<string> SubjectCodes { get; init; } = new();
}

public sealed record GetSubjectSemesterEventResponse : AbstractApiResponse<List<SubjectSemesterInfo>>
{
    public override List<SubjectSemesterInfo> Response { get; set; } = new();
}

public sealed record SubjectSemesterInfo
{
    public string MajorCode { get; init; } = string.Empty;
    public string SubjectCode { get; init; } = string.Empty;
    public short? SemesterIndex { get; init; }
    public int? SubjectIndex { get; init; }
}
