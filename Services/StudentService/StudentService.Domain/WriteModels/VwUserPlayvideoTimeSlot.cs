namespace StudentService.Domain.WriteModels
{
    public partial class VwUserPlayvideoTimeSlot
    {
        public Guid StudentId { get; set; }
        public Guid CourseId { get; set; }
        public string Slot { get; set; } = null!;
        public long PlayCount { get; set; }
    }
}
