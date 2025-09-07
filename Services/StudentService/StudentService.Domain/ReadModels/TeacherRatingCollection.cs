using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class TeacherRatingCollection
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
    
    public TeacherCollection? Teacher { get; set; }
    
    public StudentCollection? Student { get; set; }
    
    public static TeacherRatingCollection FromWriteModel(TeacherRating model, bool includeRelated = false)
    {
        var result = new TeacherRatingCollection
        {
            RatingId = model.RatingId,
            TeacherId = model.TeacherId,
            StudentId = model.StudentId,
            Rating = model.Rating,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
        };
        
        if (includeRelated)
        {
            result.Teacher = TeacherCollection.FromWriteModel(model.Teacher!);
            result.Student = StudentCollection.FromWriteModel(model.Student);
        }
        return result;
    }
    
    public static List<TeacherRatingCollection> FromWriteModel(IEnumerable<TeacherRating> models, bool includeRelated = false)
    {
        return models.Select(m => FromWriteModel(m, includeRelated)).ToList();
    }
}