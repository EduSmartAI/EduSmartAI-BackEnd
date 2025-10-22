using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class CourseSuggestionCollection
{
    public Guid CourseSuggestionId { get; set; }

    public Guid StudentId { get; set; }

    public Guid OriginalCourseId { get; set; }

    public Guid SuggestedCourseId { get; set; }

    public string Reason { get; set; } = null!;

    public bool? IsAccepted { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public static CourseSuggestionCollection FromWriteModel(CourseSuggestion writeModel)
    {
        return new CourseSuggestionCollection
        {
            CourseSuggestionId = writeModel.CourseSuggestionId,
            StudentId = writeModel.StudentId,
            OriginalCourseId = writeModel.OriginalCourseId,
            SuggestedCourseId = writeModel.SuggestedCourseId,
            Reason = writeModel.Reason,
            IsAccepted = writeModel.IsAccepted,
            CreatedAt = writeModel.CreatedAt,
            UpdatedAt = writeModel.UpdatedAt,
            CreatedBy = writeModel.CreatedBy,
            UpdatedBy = writeModel.UpdatedBy,
            IsActive = writeModel.IsActive
        };
    }
}

