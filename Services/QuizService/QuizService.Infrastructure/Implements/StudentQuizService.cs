using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseMajorSemesterSelectEvents;
using BuildingBlocks.Messaging.Events.StudentInformationInsertEvents;
using MassTransit;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class StudentQuizService : IStudentQuizService
{
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IRequestClient<StudentMajorSemesterInformationEvent> _requestStudentMajorSemesterClient;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQuizCommandRepository"></param>
    /// <param name="studentQuizQueryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="quizQueryRepository"></param>
    /// <param name="requestStudentMajorSemesterClient"></param>
    /// <param name="requestCourseMajorSemesterClient"></param>
    public StudentQuizService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository, 
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<QuizCollection> quizQueryRepository, 
        IRequestClient<StudentMajorSemesterInformationEvent> requestStudentMajorSemesterClient, 
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient)
    {
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizQueryRepository = quizQueryRepository;
        _requestStudentMajorSemesterClient = requestStudentMajorSemesterClient;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
    }
    
    #region Management Student Survey
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
        var surveyIdsRequest = request.StudentSurveys.Select(s => s.SurveyId).ToList();
        var surveyExist = await _quizQueryRepository.ToListAsync(x => surveyIdsRequest.Contains(x.QuizId) && x.QuizType == (byte) ConstantEnum.TestType.Survey && x.IsActive);
        if (!surveyExist.Any())
        {
            response.SetMessage(MessageId.E00000, "Khảo sát không tồn tại");
            return response;
        }
        
        // Get current user
        var currentUser = _identityService.GetCurrentUser();
        
        // Check student has already taken the survey
        var studentQuizExist = await _studentQuizCommandRepository.FirstOrDefaultAsync(
            x => surveyIdsRequest.Contains(x.QuizId) && x.StudentId == currentUser!.UserId && x.IsActive, 
            cancellationToken);
        if (studentQuizExist != null)
        {
            response.SetMessage(MessageId.E00000, "Khảo sát đã được điền");
            return response;
        }
        
        // Check questions and answers exist
        var requestedQuestionIds = request.StudentSurveys
            .SelectMany(s => s.Answers.Select(a => a.QuestionId))
            .Distinct()
            .ToList();
        
        var requestedAnswerIds = request.StudentSurveys
            .SelectMany(s => s.Answers.Select(a => a.AnswerId))
            .Distinct()
            .ToList();
        
        var validQuestionIds = surveyExist
            .SelectMany(q => q.Questions)
            .Select(q => q.QuestionId)
            .ToHashSet();
        
        var validAnswerIds = surveyExist
            .SelectMany(q => q.Questions)
            .SelectMany(q => q.Answers)
            .Select(a => a.AnswerId)
            .ToHashSet();
        
        if (requestedQuestionIds.Any(id => !validQuestionIds.Contains(id)) || requestedAnswerIds.Any(id => id.HasValue && !validAnswerIds.Contains(id.Value)))
        {
            response.SetMessage(MessageId.E00000, "Câu hỏi hoặc đáp án không tồn tại trong khảo sát");
            return response;
        }
        
        // Build a dictionary of valid answers for each question
        var questionAnswerMap = surveyExist
            .SelectMany(q => q.Questions)
            .ToDictionary(
                q => q.QuestionId,
                q => q.Answers.Select(a => a.AnswerId).ToHashSet()
            );
        
        // Check if each requested AnswerId belongs to its QuestionId
        foreach (var studentSurvey in request.StudentSurveys)
        {
            foreach (var answer in studentSurvey.Answers)
            {
                if (answer.AnswerId != null)
                {
                    if (!questionAnswerMap.TryGetValue(answer.QuestionId, out var validAnswers) ||
                        !validAnswers.Contains(answer.AnswerId.Value))
                    {
                        response.SetMessage(MessageId.E00000, "Đáp án không thuộc về câu hỏi trong khảo sát");
                        return response;
                    }
                }
            }
        }

        var allQuestions = surveyExist.SelectMany(q => q.Questions).ToList();
        var allAnswers = allQuestions.SelectMany(q => q.Answers).ToList();

        // Begin transaction
        var studentQuizzes = new List<StudentQuiz>();
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            foreach (var studentSurvey in request.StudentSurveys)
            {
                var newStudentQuiz = new StudentQuiz
                {
                    QuizId = studentSurvey.SurveyId,
                    StudentId = currentUser!.UserId,
                    StudentQuizAnswers = studentSurvey.Answers.Select(ans =>
                    {
                        // Find the question to get its type
                        var question = surveyExist
                            .SelectMany(q => q.Questions)
                            .FirstOrDefault(q => q.QuestionId == ans.QuestionId);
                        if (question!.QuestionType == (short) ConstantEnum.QuestionType.TrueFalse)
                        {
                            return new StudentQuizAnswer
                            {
                                QuestionId = ans.QuestionId,
                                AnswerId = ans.AnswerId,
                                AnswerText = null
                            };
                        }
                        else if (question.QuestionType == (short) ConstantEnum.QuestionType.ShortAnswer)
                        {
                            return new StudentQuizAnswer
                            {
                                QuestionId = ans.QuestionId,
                                AnswerId = null,
                                AnswerText = ans.AnswerText
                            };
                        }
                        else
                        {
                            return new StudentQuizAnswer
                            {
                                QuestionId = ans.QuestionId,
                                AnswerId = ans.AnswerId,
                                AnswerText = null
                            };
                        }
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
                var quizCollection = surveyExist.First(q => q.QuizId == studentQuiz.QuizId);
                
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
            
            // Send message to CourseService to get Major and Semester name
            var majorSemesterSelect = new CourseMajorSemesterSelectEvent
            {
                MajorId = request.StudentInformation.MajorId,
                SemesterId = request.StudentInformation.SemesterId,
            };

            // Set response from CourseService
            var messageSelectResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(majorSemesterSelect, cancellationToken);
            if (!messageSelectResponse.Message.Success)
            {
                response.MessageId = messageSelectResponse.Message.MessageId;
                response.Message = messageSelectResponse.Message.Message;
                return false;
            }
            
            // Send message to StudentService to update student information
            var majorSemesterInfoInsertEvent = new StudentMajorSemesterInformationEvent
            {
                StudentId = currentUser.UserId,
                MajorId = request.StudentInformation.MajorId,
                SemesterId = request.StudentInformation.SemesterId,
                MajorName = messageSelectResponse.Message.Response.MajorName,
                SemesterName = messageSelectResponse.Message.Response.SemesterName,
                ProgramingLanguages = request.StudentInformation.TechnologyIds,
                LearningGoalIds = request.StudentInformation.LearningGoalIds
            };

            // Request and get response
            var messageInsertResponse = await _requestStudentMajorSemesterClient.GetResponse<StudentInformationMajorSemesterEventResponse>(majorSemesterInfoInsertEvent, cancellationToken);
            if (!messageInsertResponse.Message.Success)
            {
                response.MessageId = messageInsertResponse.Message.MessageId;
                response.Message = messageInsertResponse.Message.Message;
                return false;
            }

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
    
    #endregion
}