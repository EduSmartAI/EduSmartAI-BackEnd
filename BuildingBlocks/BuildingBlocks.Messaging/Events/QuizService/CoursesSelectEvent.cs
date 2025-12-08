using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;

namespace BuildingBlocks.Messaging.Events.QuizService;

public class CoursesSelectEvent
{
    public List<string> MajorCodes { get; set; } = null!;

    public Guid SemesterId { get; set; }
    
    public int LimitTime { get; set; }
    public short StudentLevel { get; set; }
    public List<string>? StudentPassedSubjects { get; set; }
    
    public required List<CourseImprove>? CourseImproves { get; set; }
    
    public List<StudentTranscrptEvent>? StudentTranscriptSelectEvent { get; set; }
    
    public required Guid StudentId { get; set; }
    
    public required List<Guid> LearningPathCourseExists { get; set; }
};

public record CoursesSelectEventResponse : AbstractApiResponse<List<CoursesSelectEventResponseEntity>>
{
    public override List<CoursesSelectEventResponseEntity> Response { get; set; }
    
    public List<StudentCurriculumEvent> StudentCurriculums { get; set; }
}

public class StudentCurriculumEvent
{
    public string SubjectCode { get; set; } = null!;
    
    public ConstantEnum.StudentTranscriptStatus Status { get; set; }
}

public class CoursesSelectEventResponseEntity
{
    public required string MajorCode { get; set; }
    
    public required string MajorName { get; set; }
    public required List<CoursesSelectEventCourseResponseEntity> Courses { get; set; }
}

public class CoursesSelectEventCourseResponseEntity
{
    public required Guid CourseId { get; set; }
    
    public required string SubjectCode { get; set; }
    
    public required short? Level { get; set; }
}

public class CourseImprove
{
    public string SubjectCode { get; set; } = null!;

    public string? SubjectPrerequisiteCode { get; set; }

    public int Level { get; set; }
}