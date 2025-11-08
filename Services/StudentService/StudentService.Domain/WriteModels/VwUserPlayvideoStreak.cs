namespace StudentService.Domain.WriteModels
{
    public partial class VwUserPlayvideoStreak
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public long StreakDays { get; set; }
    }
}
