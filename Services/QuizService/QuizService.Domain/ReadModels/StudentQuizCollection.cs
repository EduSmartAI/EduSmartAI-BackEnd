using BaseService.Domain.Snapshort;
using QuizService.Domain.WriteModels;

namespace QuizService.Domain.ReadModels;

public class StudentQuizCollection
{
    public Guid StudentQuizId { get; set; }

    public Guid StudentId { get; set; }

    public Guid QuizId { get; set; }
    
    public short QuizType { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string CreatedBy { get; set; } = null!;

    public string UpdatedBy { get; set; } = null!;

    public bool IsActive { get; set; }

	public Guid? CourseId { get; set; }

	public short? Scope { get; set; }

	public Guid? ScopeId { get; set; }

	public short? TotalQuestions { get; set; }

	public short? TotalCorrect { get; set; }

	public short? Score100 { get; set; }

	public QuizCollection Quiz { get; set; }
	
	public UserInformation Student { get; set; }

    public virtual ICollection<StudentQuizAnswerCollection> StudentQuizAnswers { get; set; } = new List<StudentQuizAnswerCollection>();
    
    public static StudentQuizCollection FromWriteModel(StudentQuiz studentQuiz, QuizCollection quiz, UserInformation user)
    {
        var result = new StudentQuizCollection
        {
            StudentQuizId = studentQuiz.StudentQuizId,
            StudentId = studentQuiz.StudentId,
            Student = user,
            QuizId = studentQuiz.QuizId,
            QuizType = studentQuiz.QuizType,
            IsActive = studentQuiz.IsActive,
            CreatedAt = studentQuiz.CreatedAt,
            UpdatedAt = studentQuiz.UpdatedAt,
            CreatedBy = studentQuiz.CreatedBy,
            UpdatedBy = studentQuiz.UpdatedBy,
            CourseId = studentQuiz.CourseId,
            Scope = studentQuiz.Scope,
            ScopeId = studentQuiz.ScopeId,
            TotalQuestions = studentQuiz.TotalQuestions,
            TotalCorrect = studentQuiz.TotalCorrect,
            Score100 = studentQuiz.Score100,
			Quiz = quiz
        };
        foreach (var answer in studentQuiz.StudentQuizAnswers)
        {
            result.StudentQuizAnswers.Add(StudentQuizAnswerCollection.FromWriteModel(answer));
        }
        return result;
    }
}