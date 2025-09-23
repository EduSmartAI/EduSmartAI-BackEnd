namespace BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;

public class StudentMajorSemesterInformationEvent
{
    public Guid StudentId { get; set; }
    
    public Guid SemesterId { get; set; }
    
    public string SemesterName { get; set; }
    
    public Guid MajorId { get; set; }
    
    public string MajorName { get; set; }
    
    public List<Guid> ProgramingLanguages { get; set; }
    
    public Guid LearningGoalId { get; set; }
    
    public StudentMajorOrientation StudentMajorOrientation { get; set; }
}

public class StudentMajorOrientation
{
    public List<MajorInternal> MajorInternals { get; set; } = null!;
    
    public List<MajorExternal> MajorExternals { get; set; } = null!;
}

public class MajorInternal
{
    public string MajorName { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}

public class MajorExternal
{
    public string MajorName { get; set; } = null!;
    
    public string Reason { get; set; } = null!;
}