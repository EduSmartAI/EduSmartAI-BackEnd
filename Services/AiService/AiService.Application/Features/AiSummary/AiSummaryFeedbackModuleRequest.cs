using MediatR;

namespace AiService.Application.Features.AiSummary
{
    public class AiSummaryFeedbackModuleRequest : IRequest<AiSummaryFeedbackModuleResponse>
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid ModuleId { get; set; }
    }
    public class AiSummaryFeedbackModuleDto
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid ModuleId { get; set; }
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
