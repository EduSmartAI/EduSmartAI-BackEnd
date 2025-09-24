using System;
using System.Collections.Generic;

namespace StudentService.Domain.WriteModels;

public partial class Student
{
    public Guid StudentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? PhoneNumber { get; set; }

    public short? Gender { get; set; }

    public string? AvatarUrl { get; set; }

    public string? Address { get; set; }

    public Guid? MajorId { get; set; }

    public string? Bio { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool IsActive { get; set; }

    public Guid? SemesterId { get; set; }

    public virtual ICollection<LearningPath> LearningPaths { get; set; } = new List<LearningPath>();

    public virtual ICollection<StudentLearningGoal> StudentLearningGoals { get; set; } = new List<StudentLearningGoal>();

    public virtual ICollection<StudentOrientation> StudentOrientations { get; set; } = new List<StudentOrientation>();

    public virtual ICollection<StudentTechnology> StudentTechnologies { get; set; } = new List<StudentTechnology>();
}
