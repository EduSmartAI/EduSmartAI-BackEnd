using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels;

public sealed class StudentCollection
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
    public int? SemesterId { get; set; }
    public string? Bio { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsActive { get; set; }

    public MajorCollection? Major { get; set; }
    public SemesterCollection? Semester { get; set; }
    
    public ICollection<LearningGoalCollection> LearningGoals { get; set; } = new List<LearningGoalCollection>();

    public static StudentCollection FromWriteModel(Student model, bool included = false)
    {
        var studentCollection = new StudentCollection
        {
            StudentId = model.StudentId,
            FirstName = model.FirstName,
            LastName = model.LastName,
            DateOfBirth = model.DateOfBirth,
            PhoneNumber = model.PhoneNumber,
            Gender = model.Gender,
            AvatarUrl = model.AvatarUrl,
            Address = model.Address,
            MajorId = model.MajorId,
            SemesterId = model.SemesterId,
            Bio = model.Bio,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            CreatedBy = model.CreatedBy,
            UpdatedBy = model.UpdatedBy,
            IsActive = model.IsActive
        };

        if (included)
        {
            if (model.Major != null)
            {
                studentCollection.Major = MajorCollection.FromWriteModel(model.Major);
            }

            if (model.Semester != null)
            {
                studentCollection.Semester = SemesterCollection.FromWriteModel(model.Semester);
            }
            
            if (model.StudentLearningGoals.Any())
            {
                studentCollection.LearningGoals = model.StudentLearningGoals
                    .Where(sl => sl.Goal != null)
                    .Select(sl => LearningGoalCollection.FromWriteModel(sl.Goal!))
                    .ToList();
            }
        }

        return studentCollection;
    }
}