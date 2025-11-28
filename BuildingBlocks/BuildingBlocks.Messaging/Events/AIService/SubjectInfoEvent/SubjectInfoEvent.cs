using BaseService.Common.ApiEntities;
using System.Collections.Generic;

namespace BuildingBlocks.Messaging.Events.AIService.SubjectInfoEvent
{
    public sealed record SubjectInfoEvent
    {
        public string MajorCode { get; init; } = string.Empty;
        public List<string> SubjectCodes { get; init; } = new();
    }

    public sealed record SubjectInfoEventResponse : AbstractApiResponse<List<SubjectInfoItem>>
    {
        public override List<SubjectInfoItem> Response { get; set; } = new();
    }

    public sealed record SubjectInfoItem
    {
        public string MajorCode { get; init; } = string.Empty;
        public short? SemesterIndex { get; init; }
        public int SubjectIndex { get; init; }
        public string SubjectCode { get; init; } = string.Empty;
        public string SubjectName { get; init; } = string.Empty;
        public List<string> PrereqSubjectCodes { get; init; } = new();
    }
}
