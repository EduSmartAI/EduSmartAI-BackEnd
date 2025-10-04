using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public class AnswerRuleCollection
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

    public static AnswerRuleCollection FromWriteModel(AnswerRule model)
    {
        return new AnswerRuleCollection
        {
            RuleId = model.RuleId,
            AnswerId = model.AnswerId,
            NumericMin = model.NumericMin,
            NumericMax = model.NumericMax,
            Unit = model.Unit,
            MappedField = model.MappedField,
            Formula = model.Formula,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };
    }
}
