using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.Admin.Queries.Tests;

public record AdminSelectPlacementTestQueryResponse : AbstractApiResponse<AdminSelectPlacementTestQueryResponseEntity>
{
    public override AdminSelectPlacementTestQueryResponseEntity Response { get; set; }
}

public class AdminSelectPlacementTestQueryResponseEntity
{
    public Guid TestId { get; set; }
    
    public string TestName { get; set; } = null!;

    public string? Description { get; set; }
    
    public int TotalStudentAnswered { get; set; }
    
    public List<AdminSelectQuizDetailResponse> Quizzes { get; set; } = null!;
}

public class AdminSelectQuizDetailResponse
{
    public Guid QuizId { get; set; }
    
    public string? Title { get; set; }

    public string? Description { get; set; }
    
    public Guid? SubjectCode { get; set; }
    
    public string? SubjectCodeName { get; set; }
    
    public int TotalQuestions { get; set; }
    
    // public List<AdminSelectStudentQuizAnswerDetailResponse> StudentQuizAnswers { get; set; } = null!;
    
    public List<AdminSelectQuestionDetailResponse> Questions { get; set; } = null!;
}

public class AdminSelectQuestionDetailResponse
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public short QuestionType { get; set; }
    
    public string QuestionTypeName { get; set; } = null!;
    
    public short? DifficultyLevel { get; set; }
    
    public List<AdminSelectAnswerDetailResponse> Answers { get; set; } = null!;
}

public class AdminSelectAnswerDetailResponse
{
    public Guid AnswerId { get; set; }
    
    public string AnswerText { get; set; } = null!;
    
    public bool IsCorrect { get; set; }
}

public class AdminSelectStudentQuizAnswerDetailResponse
{
    public Guid StudentQuizAnswerId { get; set; }
    
    public StudentInformation Student { get; set; } = null!;
    
    public AnsweredQuestionDetail AnsweredQuestion { get; set; } = null!;

    public class StudentInformation
    {
        public Guid StudentId { get; set; }
    
        public string? Email { get; set; } = null!;
    
        public string? FullName { get; set; } = null!;
    }
    
    public class AnsweredQuestionDetail
    {
        public Guid QuestionId { get; set; }
    
        public Guid? AnswerId { get; set; }
    
        public string? AnswerText { get; set; }
    }
}