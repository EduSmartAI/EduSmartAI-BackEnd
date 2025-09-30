using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class AnswerRule
{
    public Guid RuleId { get; set; }

    public Guid AnswerId { get; set; }

    public int? NumericMin { get; set; }

    public int? NumericMax { get; set; }

    public string? Unit { get; set; }

    public string? MappedField { get; set; }

    public string? Formula { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Answer Answer { get; set; } = null!;
}
