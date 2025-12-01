﻿using MediatR;

namespace AiService.Application.Features.AiRecommend
{
    public class AiRecommendImprovementRequest : IRequest<AiRecommendImprovementResponse>
    {
        public string CareerGoal { get; set; } = string.Empty;
        public required List<SubjectMark> SubjectMarks { get; set; }
        public required List<AbilityMark>? AbilityMarks { get; set; }
        
        public List<MajorInfo> Majors { get; set; } = new();
        
        public required QuizSurvey QuizSurvey { get; set; }
    }
    
    public class MajorInfo
    {
        public string MajorCode { get; set; } = string.Empty;
        public string MajorName { get; set; } = string.Empty;
    }
    public class SubjectMark
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public double? Mark { get; set; }
    }
    public class AbilityMark
    {
        public string Name { get; set; } = string.Empty;
        public double Mark { get; set; }
    }
    public class SubjectCur
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public int Index { get; set; }
        public List<string> SubjectPrerequisiteCode { get; set; } = [];
    }
    public class QuizSurvey
    {
        public List<QuizInterest> QuizInterests { get; set; } = [];
        public List<QuizHabit> QuizHabits { get; set; } = [];
    }
    public class QuizInterest
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }
    public class QuizHabit
    {
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
    }

}
