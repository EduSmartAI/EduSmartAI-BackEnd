namespace StudentService.Domain.Models;

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

    public short? Marjor { get; set; }

    public short? Semester { get; set; }

    public string? Bio { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string? CreatedBy { get; set; }

    public string? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }
}
