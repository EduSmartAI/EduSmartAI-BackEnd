using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class ProblemExample
{
    public Guid ExampleId { get; set; }

    public Guid? ProblemId { get; set; }

    public int ExampleOrder { get; set; }

    public string InputData { get; set; } = null!;

    public string OutputData { get; set; } = null!;

    public string? Explanation { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual Problem? Problem { get; set; }
}
