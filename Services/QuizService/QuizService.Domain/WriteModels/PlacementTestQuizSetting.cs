using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class PlacementTestQuizSetting
{
    public Guid QuizId { get; set; }

    public Guid SubjectCode { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Quiz Quiz { get; set; } = null!;
}
