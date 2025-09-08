
namespace StudentService.Domain.WriteModels;

public partial class TeacherRating
{
    public Guid RatingId { get; set; }

    public Guid? TeacherId { get; set; }

    public short? Rating { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public Guid StudentId { get; set; }

    public virtual Student Student { get; set; }

    public virtual Teacher? Teacher { get; set; }
}
