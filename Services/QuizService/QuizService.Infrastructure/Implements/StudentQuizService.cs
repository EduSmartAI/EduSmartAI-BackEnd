using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class StudentQuizService : IStudentQuizService
{
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly ICommandRepository<StudentQuizAnswer> _studentQuizAnswerCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQuizCommandRepository"></param>
    /// <param name="studentQuizAnswerCommandRepository"></param>
    /// <param name="studentQuizQueryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    public StudentQuizService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        ICommandRepository<StudentQuizAnswer> studentQuizAnswerCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository, IIdentityService identityService,
        IUnitOfWork unitOfWork, IQueryRepository<QuizCollection> quizQueryRepository)
    {
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizAnswerCommandRepository = studentQuizAnswerCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizQueryRepository = quizQueryRepository;
    }

    /// <summary>
    /// Insert student survey
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentSurveyInsertResponse> InsertStudentSurveyAsync(StudentSurveyInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentSurveyInsertResponse { Success = false };

        // Check if the quiz has already been taken
        var quizIds = request.StudentSurveys.Select(s => s.SurveyId).ToList();
        var quizExist = await _quizQueryRepository.ToListAsync(x => quizIds.Contains(x.QuizId) && x.IsActive);
        if (quizExist.Any(x => x.QuizType != (byte)ConstantEnum.TestType.Survey))
        {
            response.SetMessage(MessageId.E00000, "Khảo sát không tồn tại");
            return response;
        }
        
        var currentUser = _identityService.GetCurrentUser();
        
        // Check student has already taken the survey
        var studentQuizExist = await _studentQuizCommandRepository.FirstOrDefaultAsync(
            x => quizIds.Contains(x.QuizId) && x.StudentId == currentUser!.UserId && x.IsActive, 
            cancellationToken);
        if (studentQuizExist != null)
        {
            response.SetMessage(MessageId.E00000, "Khảo sát đã được điền");
            return response;
        }

        var allQuestions = quizExist.SelectMany(q => q.Questions).ToList();
        var allAnswers = allQuestions.SelectMany(q => q.Answers).ToList();

        // Begin transaction
        var studentQuizzes = new List<StudentQuiz>();
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            foreach (var studentSurvey in request.StudentSurveys)
            {
                // Insert new student quiz to Write Model
                var newStudentQuiz = new StudentQuiz
                {
                    QuizId = studentSurvey.SurveyId,
                    StudentId = currentUser!.UserId,
                    StudentQuizAnswers = studentSurvey.Answers.Select(ans => new StudentQuizAnswer
                    {
                        QuestionId = ans.QuestionId,
                        AnswerId = ans.AnswerId,
                        AnswerText = ans.AnswerText
                    }).ToList()
                };
                studentQuizzes.Add(newStudentQuiz);
            }

            // Insert into database
            await _studentQuizCommandRepository.AddRangeAsync(studentQuizzes);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);

            // Insert denormalized data into Marten document store for optimized reading
            foreach (var studentQuiz in studentQuizzes)
            {
                // Get related quiz
                var quizCollection = quizExist.First(q => q.QuizId == studentQuiz.QuizId);
                
                // Map to StudentQuizCollection
                var studentQuizCollection = new StudentQuizCollection
                {
                    StudentQuizId = studentQuiz.StudentQuizId,
                    StudentId = studentQuiz.StudentId,
                    QuizId = studentQuiz.QuizId,
                    IsActive = studentQuiz.IsActive,
                    CreatedAt = studentQuiz.CreatedAt,
                    UpdatedAt = studentQuiz.UpdatedAt,
                    CreatedBy = studentQuiz.CreatedBy,
                    UpdatedBy = studentQuiz.UpdatedBy,
                    Quiz = quizCollection,
                    StudentQuizAnswers = studentQuiz.StudentQuizAnswers.Select(x =>
                    {
                        var questionCollection = allQuestions.FirstOrDefault(q => q.QuestionId == x.QuestionId);
                        var answerCollection = allAnswers.FirstOrDefault(a => a.AnswerId == x.AnswerId);

                        return new StudentQuizAnswerCollection
                        {
                            StudentQuizAnswerId = x.StudentQuizAnswerId,
                            StudentQuizId = x.StudentQuizId,
                            QuestionId = x.QuestionId,
                            AnswerId = x.AnswerId,
                            AnswerText = x.AnswerText,
                            IsActive = x.IsActive,
                            CreatedAt = x.CreatedAt,
                            UpdatedAt = x.UpdatedAt,
                            CreatedBy = x.CreatedBy,
                            UpdatedBy = x.UpdatedBy,
                            Question = questionCollection,
                            Answer = answerCollection
                        };
                    }).ToList()
                };

                // Store to Marten
                _unitOfWork.Store(studentQuizCollection);
            }
            
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Ghi nhận câu trả lời của sinh viên");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Select student survey
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<StudentSurveySelectResponse> SelectStudentSurveyAsync(StudentSurveySelectQuery request)
    {
        var response = new StudentSurveySelectResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser();

        string cacheKey = $"surveys:student:{currentUser!.UserId}";

        // Get surveys from cache or database
        var studentSurvey = await _studentQuizQueryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _studentQuizQueryRepository.ToListAsync(x =>
                    x.StudentId == currentUser.UserId && x.IsActive);
            },
            TimeSpan.FromMinutes(10));

        var studentSurveyResponse = studentSurvey.Select(x => new StudentSurveySelectResponseEntity
        {
            StudentSurveyId = x.StudentQuizId,
            Survey = new StudentSurveySelectQuizResponseEntity
            {
                Title = x.Quiz.Title,
                Description = x.Quiz.Description,
                Questions = x.Quiz.Questions.Select(ques => new StudentSurveySelectQuestionResponseEntity
                {
                    QuestionId = ques.QuestionId,
                    QuestionText = ques.QuestionText,
                    Answers = ques.Answers.Select(a => new StudentSurveySelectAnswerResponseEntity
                    {
                        AnswerId = a.AnswerId,
                        IsCorrect = a.IsCorrect,
                        AnswerText = a.AnswerText,
                    }).ToList()
                }).ToList()
            }
        }).ToList();

        // True
        response.Success = true;
        response.Response = studentSurveyResponse;
        response.SetMessage(MessageId.I00001, "Lấy khảo sát của sinh viên");
        return response;
    }
}