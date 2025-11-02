namespace StudentService.Domain.WriteModels;

public partial class AiEvaluationImprovement
{
    public Guid ImprovementId { get; set; }

    public Guid EvaluationId { get; set; }

    public int PositionIndex { get; set; }

    public string ImprovementsText { get; set; } = null!;

    public string? ContentMarkdown { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Slug { get; set; }

    public virtual AiEvaluation Evaluation { get; set; } = null!;
}
