namespace StudentService.Domain.WriteModels;

public partial class CourseSuggestion
{
    public Guid CourseSuggestionId { get; set; }

    public Guid StudentId { get; set; }

    public Guid OriginalCourseId { get; set; }

    public Guid SuggestedCourseId { get; set; }

    public string Reason { get; set; } = null!;

    public short SuggestionType { get; set; }

    public bool IsViewed { get; set; }

    public bool? IsAccepted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Student? Student { get; set; }
}

