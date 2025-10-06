using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;
using QuizService.Application.Applications.StudentSurveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class StudentSurveyService : IStudentSurveyService
{
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
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
    /// <param name="requestCourseMajorSemesterClient"></param>
    /// <param name="requestStudentInterestAnalysisClient"></param>
    /// <param name="outboxService"></param>
    public StudentSurveyService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<QuizCollection> quizQueryRepository,
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient,
        ICommandRepository<OutboxMessage> outboxService, IPublishEndpoint publishEndpoint)
    {
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizQueryRepository = quizQueryRepository;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
        _outboxService = outboxService;
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

        // Validate survey existence
        var surveyExist = await ValidateSurveyExistenceAsync(request, response);
        if (surveyExist == null || !surveyExist.Any()) return response;

        // Validate learning goal requirement
        if (!await ValidateLearningGoalAsync(request, surveyExist, response)) return response;

        // Get current user
        var currentUser = _identityService.GetCurrentUser();

        // Validate if the student has already taken the survey -> if yes, deactivate old entries
        await ValidateStudentSurveyStatusAsync(request, currentUser!.UserId, currentUser.Email, cancellationToken);

        // Validate questions and answers
        if (!ValidateQuestionsAndAnswers(request, surveyExist, response)) return response;

        // Flatten all questions and answers for outbox message
        var allQuestions = surveyExist.SelectMany(q => q.Questions).ToList();
        var allAnswers = allQuestions.SelectMany(q => q.Answers).ToList();

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Build StudentQuiz entities
            var studentQuizzes = BuildStudentQuizzes(request, currentUser!.UserId);

            await _studentQuizCommandRepository.AddRangeAsync(studentQuizzes);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            var outboxMessages = new List<OutboxMessage>();

            // Outbox for StudentQuizCollectionInsertEvent
           var studentQuizCollections = BuildOutboxForSurveyCollection(studentQuizzes, surveyExist, allQuestions, allAnswers, outboxMessages);

            // Get course, major, semester info from CourseService
            var courseInfoResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(
                    new CourseMajorSemesterSelectEvent
                    {
                        MajorId = request.StudentInformation.MajorId,
                        SemesterId = request.StudentInformation.SemesterId,
                    }, cancellationToken);

            if (!courseInfoResponse.Message.Success)
            {
                response.MessageId = courseInfoResponse.Message.MessageId;
                response.Message = courseInfoResponse.Message.Message;
                return false;
            }
            
            var majorSemesterInfoInsertEvent = new StudentMajorSemesterInformationEvent
            {
                StudentId = currentUser.UserId,
                MajorId = request.StudentInformation.MajorId,
                SemesterId = request.StudentInformation.SemesterId,
                MajorName = courseInfoResponse.Message.Response.MajorName,
                SemesterName = courseInfoResponse.Message.Response.SemesterName,
                ProgramingLanguages = request.StudentInformation.Technologies.Select(x => x.TechnologyId).ToList(),
                LearningGoalId = request.StudentInformation.LearningGoal.LearningGoalId,
            };

            // Add outbox message
            outboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentMajorSemesterInformationEvent),
                Content = JsonSerializer.Serialize(majorSemesterInfoInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            });
            
            await _outboxService.AddRangeAsync(outboxMessages);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Remove old related cache
            await _unitOfWork.CacheRemoveAsync(CacheKey.StudentMajorSemesterInformation(currentUser.UserId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.StudentSurvey(currentUser.UserId));
            
            // Add new related cache
            await _unitOfWork.CacheSetAsync(
                CacheKey.StudentMajorSemesterInformation(currentUser.UserId),
                majorSemesterInfoInsertEvent,
                TimeSpan.FromMinutes(5)
            );
            
            await _unitOfWork.CacheSetStringAsync(
                CacheKey.StudentSurvey(currentUser.UserId),
                JsonSerializer.Serialize(studentQuizCollections),
                TimeSpan.FromMinutes(5)
            );

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Ghi nhận câu trả lời của sinh viên");
            return true;
        }, cancellationToken);

        return response;
    }
    #region Private Methods

    private async Task<List<QuizCollection>?> ValidateSurveyExistenceAsync(StudentSurveyInsertCommand request,
        StudentSurveyInsertResponse response)
    {
        // Check if the quiz has already been taken
        var surveyIdsRequest = request.StudentSurveys.Select(s => s.SurveyId).ToList();
        // Get surveys from database
        var surveyExist = await _quizQueryRepository.ToListAsync(x => surveyIdsRequest.Contains(x.QuizId)
                                                                      && x.QuizType ==
                                                                      (short) ConstantEnum.TestType.Survey
                                                                      && x.IsActive);

        if (!surveyExist.Any())
        {
            response.SetMessage(MessageId.E00000, "Khảo sát không tồn tại");
            return null;
        }

        return surveyExist;
    }

    private Task<bool> ValidateLearningGoalAsync(StudentSurveyInsertCommand request,
        List<QuizCollection> surveyExist, StudentSurveyInsertResponse response)
    {
        // If learning goal type is None, check if the survey list contains the INTEREST survey
        if (request.StudentInformation.LearningGoal.LearningGoalType == (short)ConstantEnum.LearningGoalType.None &&
            surveyExist.FirstOrDefault(x =>
                x.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST)) == null)
        {
            response.SetMessage(MessageId.I00000, "Sinh viên chưa có định hướng nghề nghiệp bắt buộc phải làm khảo sát sở thích học tập");
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private async Task ValidateStudentSurveyStatusAsync(StudentSurveyInsertCommand request, Guid studentId, string email, CancellationToken cancellationToken)
    {
        // // Check student has already taken the survey
        var surveyIdsRequest = request.StudentSurveys.Select(s => s.SurveyId).ToList();
        var studentQuizExists = await _studentQuizCommandRepository
            .Find(x => surveyIdsRequest.Contains(x.QuizId)
                                      && x.StudentId == studentId
                                      && x.IsActive)
            .ToListAsync(cancellationToken: cancellationToken);
        if (studentQuizExists.Any())
        {
            _studentQuizCommandRepository.UpdateRange(studentQuizExists);
            await _unitOfWork.SaveChangesAsync(email, cancellationToken, true);
        }

        // Delete old StudentQuizCollection for the deactivated StudentQuiz
        foreach (var studentQuiz in studentQuizExists)
        {
            var studentQuizCollection = await _studentQuizQueryRepository.FirstOrDefaultAsync(x => x.QuizId == studentQuiz.QuizId && x.IsActive);
            
            _unitOfWork.Delete(studentQuizCollection!); 
        }
        await _unitOfWork.SessionSaveChangesAsync();
    }

    private bool ValidateQuestionsAndAnswers(StudentSurveyInsertCommand request, List<QuizCollection> surveyExist, StudentSurveyInsertResponse response)
    {
        // Check questions exist
        var requestedQuestionIds = request.StudentSurveys.SelectMany(s => s.Answers.Select(a => a.QuestionId))
            .Distinct().ToList();
        // Check answers exist
        var requestedAnswerIds = request.StudentSurveys.SelectMany(s => s.Answers.Select(a => a.AnswerId)).Distinct()
            .ToList();

        // Get valid question and answer IDs from the existing surveys
        var validQuestionIds = surveyExist.SelectMany(q => q.Questions).Select(q => q.QuestionId).ToHashSet();
        var validAnswerIds = surveyExist.SelectMany(q => q.Questions).SelectMany(q => q.Answers).Select(a => a.AnswerId)
            .ToHashSet();

        // Validate requested QuestionIds and AnswerIds
        if (requestedQuestionIds.Any(id => !validQuestionIds.Contains(id)) ||
            requestedAnswerIds.Any(id => !validAnswerIds.Contains(id)))
        {
            response.SetMessage(MessageId.E00000, "Câu hỏi hoặc đáp án không tồn tại trong khảo sát");
            return false;
        }

        return true;
    }

    private List<StudentQuiz> BuildStudentQuizzes(StudentSurveyInsertCommand request, Guid studentId)
    {
        // Map request to StudentQuiz entities
        return request.StudentSurveys.Select(studentSurvey => new StudentQuiz
        {
            QuizId = studentSurvey.SurveyId,
            StudentId = studentId,
            QuizType = (short)ConstantEnum.TestType.Survey,
            StudentQuizAnswers = studentSurvey.Answers.Select(ans => new StudentQuizAnswer
            {
                QuestionId = ans.QuestionId,
                AnswerId = ans.AnswerId,
            }).ToList()
        }).ToList();
    }

    private List<StudentQuizCollection> BuildOutboxForSurveyCollection(List<StudentQuiz> studentQuizzes, 
        List<QuizCollection> surveyExist,
        List<QuestionCollection> allQuestions,
        List<AnswerCollection> allAnswers,
        List<OutboxMessage> outboxMessages)
    {
        // Map to StudentQuizCollection for the event
        var studentQuizCollections = studentQuizzes.Select(studentQuiz =>
        {
            var quizCollection = surveyExist.First(q => q.QuizId == studentQuiz.QuizId);

            return new StudentQuizCollection
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
                StudentQuizAnswers = studentQuiz.StudentQuizAnswers.Select(x => new StudentQuizAnswerCollection
                {
                    StudentQuizAnswerId = x.StudentQuizAnswerId,
                    StudentQuizId = x.StudentQuizId,
                    QuestionId = x.QuestionId,
                    AnswerId = x.AnswerId,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt,
                    UpdatedAt = x.UpdatedAt,
                    CreatedBy = x.CreatedBy,
                    UpdatedBy = x.UpdatedBy,
                    Question = allQuestions.FirstOrDefault(q => q.QuestionId == x.QuestionId),
                    Answer = allAnswers.FirstOrDefault(a => a.AnswerId == x.AnswerId)
                }).ToList()
            };
        }).ToList();

        // Prepare outbox message for StudentQuizCollectionInsertEvent
        var studentQuizInsertEvent = new StudentQuizCollectionInsertEvent
        {
            StudentQuizzes = studentQuizCollections
        };

        // Return outbox message
        outboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(StudentQuizCollectionInsertEvent),
            Content = JsonSerializer.Serialize(studentQuizInsertEvent),
            OccurredOnUtc = DateTime.UtcNow,
        });
        
        return studentQuizCollections;
    }
    #endregion

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
                Title = x.Quiz.SurveyQuizSetting!.Title,
                Description = x.Quiz.SurveyQuizSetting.Description,
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
    
    private int GetStudentStudyTime(IEnumerable<StudentQuizAnswerCollection> studentQuizAnswers)
    {
        var answerRules = studentQuizAnswers
            .SelectMany(a => a.Answer!.AnswerRule!)
            .ToList();

        int? hourPerDay = null;
        int? hourPerWeek = null;
        int? daysPerWeek = null;
        int? months = null;

        foreach (var rule in answerRules)
        {
            var avg = (rule.NumericMin ?? 0) + (rule.NumericMax ?? rule.NumericMin ?? 0);
            avg /= ((rule.NumericMin.HasValue && rule.NumericMax.HasValue) ? 2 : 1);

            if (Enum.TryParse<ConstantEnum.AnswerRuleUnit>(rule.Unit, out var unit))
            {
                switch (unit)
                {
                    case ConstantEnum.AnswerRuleUnit.HourPerDay:
                        hourPerDay = avg;
                        break;
                    case ConstantEnum.AnswerRuleUnit.HourPerWeek:
                        hourPerWeek = avg;
                        break;
                    case ConstantEnum.AnswerRuleUnit.Days:
                        daysPerWeek = avg;
                        break;
                    case ConstantEnum.AnswerRuleUnit.Months:
                        months = avg;
                        break;
                }
            }
        }

        int totalMinutes = 0;

        if (hourPerDay.HasValue && daysPerWeek.HasValue && months.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * daysPerWeek.Value * 4 * months.Value;
        }
        else if (hourPerWeek.HasValue && months.HasValue)
        {
            totalMinutes = hourPerWeek.Value * 60 * 4 * months.Value;
        }
        else if (hourPerDay.HasValue && months.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * 7 * 4 * months.Value;
        }
        else if (hourPerDay.HasValue && daysPerWeek.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * daysPerWeek.Value * 4;
        }

        int totalHours = totalMinutes / 60;
        return totalHours;
    }
    
}