using System.Collections.Generic;
using System.Linq;
using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class LearningPathMajorCollection
{
    public Guid LearningPathMajorId { get; set; }
    public Guid PathId { get; set; }
    public string MajorCode { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }

    /// <summary>1: Internal, 2: External</summary>
    public short Type { get; set; }
    public int? PositionIndex { get; set; }

    public List<LearningPathCourseCollection> LearningPathCourses { get; set; }
        = new();

    public List<LearningPathSubjectCodeCollection> LearningPathSubjectCodes { get; set; }
        = new();

    public static LearningPathMajorCollection FromWriteModel(LearningPathMajor model)
    {
        var courses = (model.LearningPathCourses ?? new List<LearningPathCourse>())
            .Where(c => c.IsActive)
            .OrderBy(c => c.Position ?? int.MaxValue)
            .ToList();

        var subjectCodes = (model.LearningPathSubjectCodes ?? new List<LearningPathSubjectCode>())
            .Where(sc => sc.IsActive)
            .OrderBy(sc => sc.SubjectCode)
            .Select(sc =>
            {
                var subjectCourses = courses
                    .Where(c => c.LearningPathSubjectCodeId == sc.LearningPathSubjectCodeId);

                return LearningPathSubjectCodeCollection.FromWriteModel(sc, subjectCourses);
            })
            .ToList();

        return new LearningPathMajorCollection
        {
            LearningPathMajorId = model.LearningPathMajorId,
            PathId = model.PathId,
            MajorCode = model.MajorCode,
            Reason = model.Reason,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            Type = model.Type,
            PositionIndex = model.PositionIndex,
            LearningPathCourses = courses
                .Select(c => LearningPathCourseCollection.FromWriteModel(c))
                .ToList(),
            LearningPathSubjectCodes = subjectCodes
        };
    }

}