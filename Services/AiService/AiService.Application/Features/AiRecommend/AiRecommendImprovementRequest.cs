using MediatR;

namespace AiService.Application.Features.AiRecommend
{
    public class AiRecommendImprovementRequest : IRequest<AiRecommendImprovementResposne>
    {
        public string careerGoal { get; set; } = string.Empty;
        public required List<SubjectMark> SubjectMarks { get; set; }
        public required List<AbilityMark> AbilityMarks { get; set; }
        public string MajorCode { get; set; } = string.Empty;
        public Curriculum? Curriculum { get; set; } = new();
        public required QuizSurvey QuizSurvey { get; set; }

    }
    public class SubjectMark
    {
        public string subjectCode { get; set; } = string.Empty;
        public string subjectName { get; set; } = string.Empty;
        public int? mark { get; set; }
    }
    public class AbilityMark
    {
        public string name { get; set; } = string.Empty;
        public int mark { get; set; }
    }
    public class Curriculum
    {
        public List<SubjectCur> subjects { get; set; } = [];
    }
    public class SubjectCur
    {
        public string subjectCode { get; set; } = string.Empty;
        public string subjectName { get; set; } = string.Empty;
        public int index { get; set; }
        public List<string> subjectPrerequisiteCode { get; set; } = [];
    }
    public class QuizSurvey
    {
        public List<QuizInterest> quizInterests { get; set; } = [];
        public List<QuizHabit> quizHabits { get; set; } = [];
    }
    public class QuizInterest
    {
        public string question { get; set; } = string.Empty;
        public string answer { get; set; } = string.Empty;
    }
    public class QuizHabit
    {
        public string question { get; set; } = string.Empty;
        public string answer { get; set; } = string.Empty;
    }

}
