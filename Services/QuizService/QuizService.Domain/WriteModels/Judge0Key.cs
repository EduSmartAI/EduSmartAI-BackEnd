using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Judge0Key
{
    public string Judge0Key1 { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }
}
