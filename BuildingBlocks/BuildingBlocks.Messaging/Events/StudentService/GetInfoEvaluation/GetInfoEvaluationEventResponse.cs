using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation
{
    public sealed record GetInfoEvaluationEventResponse : AbstractApiResponse<GetInfoEvaluationGroupedDto>
    {
        public override GetInfoEvaluationGroupedDto Response { get; set; } = new();
    }

    // ✅ Payload đã group: tách 2 nhánh Lesson / Module
    public sealed class GetInfoEvaluationGroupedDto
    {
        public List<EvaluationGroupDto> Lessons { get; set; } = new();
        public List<EvaluationGroupDto> Modules { get; set; } = new();
    }

    // ✅ Một group = 1 ScopeId (LessonId/ModuleId) + list các evaluation thuộc group đó
    public sealed class EvaluationGroupDto
    {
        public Guid ScopeId { get; set; } // LessonId hoặc ModuleId
        public List<GetInfoEvaluationItemDto> Evaluations { get; set; } = new();
    }

    // ✅ Item giữ nguyên fields cần hiển thị
    public sealed class GetInfoEvaluationItemDto
    {
        public Guid EvaluationId { get; set; }
        public Guid AttemptId { get; set; }
        public Guid QuizId { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Score100 { get; set; }
        public short? Score100Raw { get; set; }

        public string Summary { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string Improvements { get; set; } = string.Empty;
        public string Actions { get; set; } = string.Empty;
        public string SkillGaps { get; set; } = string.Empty;

        public string Model { get; set; } = string.Empty;
        public string RubricVersion { get; set; } = string.Empty;
        public decimal Confidence { get; set; }

        public short Scope { get; set; }         // 1 = Lesson, 2 = Module
        public Guid ScopeId { get; set; }        // LessonId hoặc ModuleId

        public DateTime CreatedAt { get; set; }
    }
}
