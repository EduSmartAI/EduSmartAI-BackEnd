using System.Collections.Generic;
using System.Linq;
using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public class LearningPathCollection
{
    public Guid PathId { get; set; }
    public string PathName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = null!;
    public string UpdatedBy { get; set; } = null!;
    public bool IsActive { get; set; }
    public Guid? StudentId { get; set; }
    public short Status { get; set; }
    public string? SummaryFeedback { get; set; }
    public string? HabitAndInterestAnalysis { get; set; }
    public string? Personality { get; set; }
    public string? LearningAbility { get; set; }
    public string? AbilityFeedback { get; set; }
    public short Level { get; set; }
    public string LevelReason { get; set; } = null!;
    public bool IsSkipTest { get; set; }
    public int LimitTime { get; set; }
    public string? EvaluationAndImprove { get; set; }
    public List<LearningPathMajorCollection> LearningPathMajors { get; set; } = new();

    public static LearningPathCollection FromWriteModel(LearningPath model)
    {
        var majors = (model.LearningPathMajors ?? new List<LearningPathMajor>())
            .Where(m => m.IsActive)
            .OrderBy(m => m.PositionIndex ?? int.MaxValue)
            .Select(LearningPathMajorCollection.FromWriteModel)
            .ToList();

        return new LearningPathCollection
        {
            PathId = model.PathId,
            PathName = model.PathName,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive,
            StudentId = model.StudentId,
            Status = model.Status,
            SummaryFeedback = model.SummaryFeedback,
            HabitAndInterestAnalysis = model.HabitAndInterestAnalysis,
            Personality = model.Personality,
            LearningAbility = model.LearningAbility,
            AbilityFeedback = model.AbilityFeedback,
            LearningPathMajors = majors,
            Level = model.Level,
            LevelReason = model.LevelReason,
            IsSkipTest = model.IsSkipTest,
            LimitTime = model.LimitTime,
            EvaluationAndImprove = model.EvaluationAndImprove
        };
    }
}