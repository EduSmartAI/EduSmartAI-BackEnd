using TeacherService.Domain.WriteModels;

namespace TeacherService.Domain.ReadModels;

public class TeacherRatingCollection
{
    public Guid RatingId { get; set; }

    public Guid TeacherId { get; set; }

    public Guid? StudentId { get; set; }

    public short Rating { get; set; }

    public string? Review { get; set; }

    public string? ReviewTitle { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }
    
    public static TeacherRatingCollection FromWriteModel(TeacherRating rating)
    {
        var collection = new TeacherRatingCollection
        {
            RatingId = rating.RatingId,
            TeacherId = rating.TeacherId,
            StudentId = rating.StudentId,
            Rating = rating.Rating,
            Review = rating.Review,
            ReviewTitle = rating.ReviewTitle,
            CreatedAt = rating.CreatedAt,
            UpdatedAt = rating.UpdatedAt,
            CreatedBy = rating.CreatedBy,
            UpdatedBy = rating.UpdatedBy,
            IsActive = rating.IsActive,
        };
        return collection;
    }
}