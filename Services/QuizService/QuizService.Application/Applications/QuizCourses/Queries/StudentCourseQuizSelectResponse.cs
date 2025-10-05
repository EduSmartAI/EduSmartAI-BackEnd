using BaseService.Common.ApiEntities;

namespace QuizService.Application.Applications.QuizCourses.Queries;

public record StudentCourseQuizSelectResponse : AbstractApiResponse<StudentCourseQuizSelectResponseEntity>
{
    public override StudentCourseQuizSelectResponseEntity Response { get; set; }
}

public class StudentCourseQuizSelectResponseEntity
{
    public Guid QuizId { get; set; }
    
    public int TotalQuestions { get; set; }
    
    public int TotalCorrectAnswers { get; set; }
    
    public List<QuestionsCourseResultSelectResponseEntity> QuestionResults { get; set; }
}

public class QuestionsCourseResultSelectResponseEntity
{
    public Guid QuestionId { get; set; }
    
    public string QuestionText { get; set; } = null!;
    
    public short QuestionType { get; set; }
    
    public string? Explanation { get; set; }
    
    public List<StudentQuizCourseAnswerDetailResponse> Answers { get; set; } = null!;
}

public record StudentQuizCourseAnswerDetailResponse
{
    public Guid? AnswerId { get; set; }
    
    public string? AnswerText { get; set; }
    
    public bool IsCorrectAnswer { get; set; }
    
    public bool SelectedByStudent { get; set; }
}