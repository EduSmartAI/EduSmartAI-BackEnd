using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent
{
    public record GetLessonInfoResponse : AbstractApiResponse<LessonInforData>
    {
        public override LessonInforData Response { get; set; } = new LessonInforData();
    }
    public record LessonInforData
    {
        public Guid? TranscriptId { get; set; }
        public string TranscriptLanguage { get; set; } = string.Empty;
        public string TranscriptText { get; set; } = string.Empty;
        public string LessonTitle { get; set; } = string.Empty;
        public int? VideoDurationSec { get; set; }
    }
}
