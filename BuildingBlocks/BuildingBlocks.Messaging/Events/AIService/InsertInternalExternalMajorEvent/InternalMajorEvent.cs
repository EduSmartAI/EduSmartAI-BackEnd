using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.QuizService;

namespace BuildingBlocks.Messaging.Events.AIService.InsertInternalExternalMajorEvent
{
    public sealed record InternalMajorEvent(
        Guid LearningPathId,
        short StudentLevel,
        string LimitTime,
        string CurrentUserEmail,
        IReadOnlyList<InternalMajorItem> Majors,
        Guid SemesterId,
        StudentMajor StudentMajor,
        List<string>? StudentPassedSubjects,
        List<CourseImprove>? CourseImproves,
        string? CareerGoal = null,
        List<SubjectMarkForAI>? SubjectMarks = null,
        List<AbilityMarkForAI>? AbilityMarks = null,
        QuizSurveyForAI? QuizSurvey = null,
        string? StudentEmail = null
    );
        
    public sealed record InternalMajorItem(
        string MajorCode,
        string Reason
    );
    
    // ✅ NEW: Data structures for AI processing
    public sealed record SubjectMarkForAI(
        string SubjectCode,
        string SubjectName,
        double? Mark
    );
    
    public sealed record AbilityMarkForAI(
        string Name,
        double Mark
    );
    
    public sealed record QuizSurveyForAI(
        List<QuizInterestForAI> QuizInterests,
        List<QuizHabitForAI> QuizHabits
    );
    
    public sealed record QuizInterestForAI(
        string Question,
        string Answer
    );
    
    public sealed record QuizHabitForAI(
        string Question,
        string Answer
    );
    
    public record InternalMajorEventResponse : AbstractApiResponse<string>
    {
        public override string Response { get; set; }
    }
}
