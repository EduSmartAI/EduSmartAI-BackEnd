using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.AIService.ModuleProgress
{
    public record GetModuleProgresssRepsonse : AbstractApiResponse<ModuleProgressDto>
    {
        public override ModuleProgressDto Response { get; set; } = new ModuleProgressDto();
    }
    public sealed class ModuleProgressDto
    {
        public int LessonsTotal { get; set; }
        public int LessonsCompleted { get; set; }
        public double PercentCompleted { get; set; }
        public double Score100Raw { get; set; }
        public double Score100 { get; set; }
        public IEnumerable<string>? Strengths { get; set; }
        public IEnumerable<string>? Improvements { get; set; }
        public IEnumerable<string>? Actions { get; set; }
        public IEnumerable<string>? SkillGaps { get; set; }
    }
}
