using System;
using System.Collections.Generic;
using System.Linq;
using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class LearningPathSubjectCodeCollection
{
    public Guid LearningPathSubjectCodeId { get; set; }
    public string SubjectCode { get; set; } = string.Empty;
    public string? AnalysisMarkdown { get; set; }
    public string? Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; }
    public Guid LearningPathMajorId { get; set; }

    public List<LearningPathCourseCollection> LearningPathCourses { get; set; } = new();

    public static LearningPathSubjectCodeCollection FromWriteModel(
        LearningPathSubjectCode model,
        IEnumerable<LearningPathCourse>? subjectCourses = null)
    {
        var courses = subjectCourses ?? model.LearningPathCourses ?? new List<LearningPathCourse>();

        return new LearningPathSubjectCodeCollection
        {
            LearningPathSubjectCodeId = model.LearningPathSubjectCodeId,
            SubjectCode = model.SubjectCode,
            AnalysisMarkdown = model.AnalysisMarkdown,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            LearningPathMajorId = model.LearningPathMajorId,
            LearningPathCourses = courses
                .Where(c => c.IsActive)
                .OrderBy(c => c.Position ?? int.MaxValue)
                .Select(c => LearningPathCourseCollection.FromWriteModel(c, model.SubjectCode))
                .ToList()
        };
    }
}

