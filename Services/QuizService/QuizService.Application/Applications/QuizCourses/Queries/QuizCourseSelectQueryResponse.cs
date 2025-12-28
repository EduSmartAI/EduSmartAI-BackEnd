using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Queries;

public record QuizCourseSelectQueryResponse : AbstractApiResponse<QuizCourseSelectQueryResponseEntity>
{
    public override QuizCourseSelectQueryResponseEntity Response { get; set; }
}

public class QuizCourseSelectQueryResponseEntity
{
    public Guid QuizId { get; set; }
    
    public int DurationMinutes { get; set; }
    
    public int PassingScorePercentage { get; set; }
    
    public bool ShuffleQuestions { get; set; }
    
    public bool ShowResultsImmediately { get; set; }
    
    public bool AllowRetake { get; set; }
    
    public int TotalQuestions { get; set; }
    
    public List<QuizCourseSelectQuestionDetailResponse> Questions { get; set; } = null!;
}

public record QuizCourseSelectQuestionDetailResponse
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public string Explanation { get; set; }
    
    public short QuestionType { get; set; }
    
    public List<QuizCourseSelectAnswerDetailResponse> Answers { get; set; } = null!;
}

public record QuizCourseSelectAnswerDetailResponse
{
    public Guid AnswerId { get; set; }
    
    public string AnswerText { get; set; } = null!;

    public bool IsCorrect { get; set; }
}
