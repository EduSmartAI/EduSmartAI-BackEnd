using BaseService.Common.ApiEntities;
using QuizService.Application.Applications.Quizzes.Queries;

namespace QuizService.Application.Applications.StudentTests.Queries;

public record StudentTestSelectResponse : AbstractApiResponse<StudentTestSelectResponseEntity>
{
    public override StudentTestSelectResponseEntity Response { get; set; }
}

public record StudentTestSelectResponseEntity
{
    public Guid StudentTestId { get; set; }
    public Guid TestId { get; set; }
    
    public string TestName { get; set; }
    
    public string? TestDescription { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? FinishedAt { get; set; }
    
    public List<QuizResultSelectResponseEntity> QuizResults { get; set; }
}

public class QuizResultSelectResponseEntity
{
    public Guid QuizId { get; set; }
    
    public string Title { get; set; } = null!;

    public string? Description { get; set; }
    
    public Guid? SubjectCode { get; set; }
    
    public string SubjectCodeName { get; set; }
    
    public int TotalQuestions { get; set; }
    
    public short DifficultyLevel { get; set; }
    
    public List<QuestionsResultSelectResponseEntity> QuestionResults { get; set; }
}

public class QuestionsResultSelectResponseEntity
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public short QuestionType { get; set; }
    
    public short? DifficultyLevel { get; set; }
    
    public List<StudentAnswerDetailResponse> Answers { get; set; } = null!;
}

public record StudentAnswerDetailResponse
{
    public Guid? AnswerId { get; set; }
    
    public bool IsCorrectAnswer { get; set; }
    
    public bool SelectedByStudent { get; set; }
    
    public string? Explanation { get; set; }
}