using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class VLearningPathCourseDetail
{
    public Guid? PathId { get; set; }

    public string? PathName { get; set; }

    public Guid? StudentId { get; set; }

    public short? PathStatus { get; set; }

    public string? SummaryFeedback { get; set; }

    public string? HabitAndInterestAnalysis { get; set; }

    public string? Personality { get; set; }

    public string? LearningAbility { get; set; }

    public Guid? LearningPathMajorId { get; set; }

    public string? MajorCode { get; set; }

    public short? MajorType { get; set; }

    public Guid? LearningPathCourseId { get; set; }

    public Guid? InternalCourseId { get; set; }

    public int? CoursePosition { get; set; }

    public string? StepName { get; set; }

    public string? ExternalCourseLink { get; set; }

    public string? ExternalCourseReason { get; set; }

    public decimal? ExternalCourseRating { get; set; }

    public string? ExternalCourseLevel { get; set; }

    public string? ExternalCourseDuration { get; set; }

    public string? ExternalCourseProvider { get; set; }

    public short? CourseStatus { get; set; }

    public Guid? LearningPathSubjectCodeId { get; set; }

    public string? SubjectCode { get; set; }
}
