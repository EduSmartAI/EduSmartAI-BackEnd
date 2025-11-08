namespace StudentService.Domain.WriteModels
{
    public partial class VwUserVideoActionsAgg
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public Guid TargetId { get; set; }
        public string TargetType { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public long ActionCount { get; set; }
    }
}
