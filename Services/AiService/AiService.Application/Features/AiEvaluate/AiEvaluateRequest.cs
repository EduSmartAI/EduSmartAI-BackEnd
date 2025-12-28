using BuildingBlocks.Messaging.Events.QuizService;
using MediatR;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace AiService.Application.Features.AiEvaluate
{
    public class AiEvaluateRequest : IRequest<AiEvaluateResponse>
    {
        public required string CareerGoal { get; set; } = null!;
        public required List<string> KnownFrameworks { get; set; } = [];
        public required List<string> KnownLanguages { get; set; } = [];
        public string ExternalLimitTime { get; set; } = null!;
        public int KRetrieval { get; set; } = 4;
        public int ScoreThreshold { get; set; } = 60;
        
        public required IdentityEntity IdentityEntity { get; set; }
        public required Guid LearningPathId { get; set; }
        public required Guid SemesterId { get; set; }
        
        public required short StudentLevel { get; set; }
        
        public required StudentMajor StudentMajor { get; set; }
        
        public List<string>? StudentPassedSubjects { get; set; }
        public required List<CourseImprove>? CourseImproves { get; set; }
        public List<StudentSubjectMarkRequest>? SubjectMarks { get; set; }
        public List<StudentAbilityMarkRequest>? AbilityMarks { get; set; }
        public StudentQuizSurveyRequest? QuizSurvey { get; set; }
        
        public List<StudentTranscrptEvent>? StudentTranscrpts { get; set; }
        public required List<AbilityImproveEvent>? AbilityImprove { get; set; }
    }
    
    public class StudentSubjectMarkRequest
    {
        public string SubjectCode { get; set; } = null!;
        public string SubjectName { get; set; } = null!;
        public double? Mark { get; set; }
    }
    
    public class StudentAbilityMarkRequest
    {
        public string Name { get; set; } = null!;
        public double Mark { get; set; }
    }
    
    public class StudentQuizSurveyRequest
    {
        public List<StudentQuizInterestRequest> QuizInterests { get; set; } = new();
        public List<StudentQuizHabitRequest> QuizHabits { get; set; } = new();
    }
    
    public class StudentQuizInterestRequest
    {
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
    }
    
    public class StudentQuizHabitRequest
    {
        public string Question { get; set; } = null!;
        public string Answer { get; set; } = null!;
    }
}
