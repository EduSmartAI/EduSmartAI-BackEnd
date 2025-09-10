using System;
using System.Collections.Generic;

namespace QuizService.Domain.WriteModels;

public partial class Test
{
    public Guid TestId { get; set; }

    public string TestName { get; set; } = null!;

    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

    public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();

    public virtual ICollection<StudentTest> StudentTests { get; set; } = new List<StudentTest>();
}
