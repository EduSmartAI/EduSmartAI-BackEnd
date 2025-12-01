using BuildingBlocks.Messaging.Events.QuizService;
using MediatR;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace AiService.Application.Features.AiEvaluate
{
    public class AiEvaluateRequest : IRequest<AiEvaluateResponse>
    {
        public string CareerGoal { get; set; } = null!;
        public List<string> KnownFrameworks { get; set; } = [];
        public List<string> KnownLanguages { get; set; } = [];
        public string ExternalLimitTime { get; set; } = null!;
        public int KRetrieval { get; set; } = 4;
        public int ScoreThreshold { get; set; } = 60;
        
        public IdentityEntity IdentityEntity { get; set; }
        public Guid LearningPathId { get; set; }
        public Guid SemesterId { get; set; }
        
        public short StudentLevel { get; set; }
        
        public List<string>? StudentPassedSubjects { get; set; }
        public required List<CourseImprove>? CourseImproves { get; set; }
        public List<StudentSubjectMarkRequest>? SubjectMarks { get; set; }
        public List<StudentAbilityMarkRequest>? AbilityMarks { get; set; }
        public StudentQuizSurveyRequest? QuizSurvey { get; set; }
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
