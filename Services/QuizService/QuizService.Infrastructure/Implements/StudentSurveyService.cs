using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BaseService.Domain.Snapshort;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using QuizService.Application.Applications.Admin.Queries.StudentSurveys;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;
using QuizService.Application.Applications.StudentSurveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Infrastructure.Implements;

public class StudentSurveyService : IStudentSurveyService
{
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly IRequestClient<StudentTranscriptSelectEvent> _requestStudentTranscriptClient;
    private readonly IRequestClient<CoreSubjectSelectEvent> _requestCoreSubjectClient;
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
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
    /// <param name="outboxService"></param>
    /// <param name="requestStudentInterestAnalysisClient"></param>
    /// <param name="requestCoreSubjectClient"></param>
    /// <param name="requestStudentInterestAnalysisClient1"></param>
    public StudentSurveyService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<QuizCollection> quizQueryRepository,
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient,
        IRequestClient<StudentTranscriptSelectEvent> requestStudentInterestAnalysisClient,
        ICommandRepository<OutboxMessage> outboxService,
        IRequestClient<CoreSubjectSelectEvent> requestCoreSubjectClient,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient1)
    {
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizQueryRepository = quizQueryRepository;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
        _outboxService = outboxService;
        _requestCoreSubjectClient = requestCoreSubjectClient;
        _requestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient1;
        _requestStudentTranscriptClient = requestStudentInterestAnalysisClient;
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
            
            if (request.IsWantToTakeTest)
            {
                // Calculate student level if IsWantToTakeTest is true
                short studentLevel = 1;
                
                studentLevel = await CalculateStudentLevelFromTranscriptAsync(currentUser.UserId, cancellationToken);
                var learningPathId = Guid.NewGuid();
            
                var studentSurveys = await _studentQuizQueryRepository.GetOrSetListAsync(
                    CacheKey.StudentSurvey(currentUser.UserId),
                    async () => await _studentQuizQueryRepository.ToListAsync(sq => sq.StudentId == currentUser.UserId && sq.QuizType == (short) ConstantEnum.TestType.Survey),
                    TimeSpan.FromMinutes(10));
                if (!studentSurveys.Any())
                {
                    response.SetMessage(MessageId.E00000, "Sinh viên chưa hoàn thành bài khảo sát nào");
                    return false;
                }
                
                // Find HABIT survey from student quiz collections
                var surveyHabit = studentSurveys.First(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));

                // Get selected answer IDs
                var selectedAnswerIds = surveyHabit.Quiz.Questions
                    .SelectMany(q => q.Answers)
                    .Select(a => a.AnswerId)
                    .ToList();
           
                // Build StudentQuizAnswerCollection for selected answers
                var studentQuizAnswers = surveyHabit.Quiz.Questions
                    .SelectMany(q => q.Answers)
                    .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                    .Select(a => new StudentQuizAnswerCollection
                    {
                        AnswerId = a.AnswerId,
                        Answer = a,
                    })
                    .ToList();

                int limitTime = GetStudentStudyTime(studentQuizAnswers);
                
                var prepareStudentLearningProfileForAiRequest = new StudentLearningPathInsertContext
                {
                    StudentQuizCollections = studentSurveys,
                    CurrentUser = currentUser,
                    InformationResponse = new StudentInformationSelectsEventResponseEntity
                    {
                        SemesterId = request.StudentInformation.SemesterId,
                        LearningGoalName = request.StudentInformation.LearningGoal.LearningGoalName,
                        LearningGoalType = request.StudentInformation.LearningGoal.LearningGoalType,
                        Technologies = request.StudentInformation.Technologies.Select(x => new StudentTechnologySelectsEventResponseEntity
                        {
                            TechnologyName = x.TechnologyName,
                            TechnologyType = x.TechnologyType
                        }).ToList(),
                    },
                    Response = response,
                    LearningPathId = learningPathId,
                    LimitTime = limitTime,
                    StudentLevel = studentLevel
                };
                
                // Publish Message to AIService
                var studentMajorOrientationEvent = await PrepareStudentLearningProfileForAiAsync(prepareStudentLearningProfileForAiRequest, cancellationToken);
                if (!studentMajorOrientationEvent.Success)
                {
                    response.MessageId = studentMajorOrientationEvent.MessageId;
                    response.Message = studentMajorOrientationEvent.Message;
                    return false;
                }
                
                // True
                response.Response = learningPathId;
            }
            
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
                QuizType = studentQuiz.QuizType,
                StudentId = studentQuiz.StudentId,
                Student = new UserInformation {Email = _identityService.GetCurrentUser()!.Email, FullName = _identityService.GetCurrentUser()!.FullName},
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
    
    /// <summary>
    /// Select student survey detail
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<StudentSurveySelectDetailResponse> SelectStudentSurveyDetailAsync(StudentSurveySelectDetailQuery request)
    {
        var response = new StudentSurveySelectDetailResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser();
        
        // Validate student survey ownership
        var ownershipCheck = await _studentQuizQueryRepository.FirstOrDefaultAsync(x => 
            x.StudentQuizId == request.StudentSurveyId && 
            x.StudentId == currentUser!.UserId &&
            x.QuizType == (short)ConstantEnum.TestType.Survey);
            
        if (ownershipCheck == null)
        {
            response.SetMessage(MessageId.E00000, "Khảo sát không thuộc về sinh viên hiện tại");
            return response;
        }

        var cacheKey = CacheKey.StudentSurvey(request.StudentSurveyId);
        var result = await GetStudentSurveyDetailAsync(request.StudentSurveyId, cacheKey);
        
        if (result == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát của sinh viên");
            return response;
        }

        response.Success = true;
        response.Response = result;
        response.SetMessage(MessageId.I00001, "Lấy thông tin chi tiết khảo sát của sinh viên");
        return response;
    }

    /// <summary>
    /// Select student survey detail for admin (no ownership check)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<AdminStudentSurveySelectDetailResponse> SelectAdminStudentSurveyDetailAsync(AdminStudentSurveySelectDetailQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminStudentSurveySelectDetailResponse { Success = false };
        
        var cacheKey = CacheKey.StudentSurvey(request.StudentSurveyId);
        var result = await GetStudentSurveyDetailAsync(request.StudentSurveyId, cacheKey);
        
        if (result == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát của sinh viên");
            return response;
        }

        response.Success = true;
        response.Response = result;
        response.SetMessage(MessageId.I00001, "Lấy thông tin chi tiết khảo sát của sinh viên");
        return response;
    }

    /// <summary>
    /// Get student survey detail - shared logic
    /// </summary>
    /// <param name="studentSurveyId">Student survey ID</param>
    /// <param name="cacheKey">Cache key to use</param>
    /// <returns>Student survey detail entity or null if not found</returns>
    private async Task<StudentSurveySelectDetailResponseEntity?> GetStudentSurveyDetailAsync(Guid studentSurveyId, string cacheKey)
    {
        // Get student survey from cache or database
        var studentSurvey = await _studentQuizQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                return await _studentQuizQueryRepository.FirstOrDefaultAsync(x => 
                    x.StudentQuizId == studentSurveyId && 
                    x.QuizType == (short)ConstantEnum.TestType.Survey &&
                    x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        
        if (studentSurvey == null)
        {
            return null;
        }

        // Get survey info
        var survey = await _quizQueryRepository.FirstOrDefaultAsync(x => x.QuizId == studentSurvey.QuizId);
        if (survey == null)
        {
            return null;
        }

        // Build question results
        var questionResults = BuildSurveyQuestionResults(survey, studentSurvey);

        return new StudentSurveySelectDetailResponseEntity
        {
            StudentSurveyId = studentSurvey.StudentQuizId,
            SurveyId = studentSurvey.QuizId,
            SurveyTitle = survey.SurveyQuizSetting?.Title ?? string.Empty,
            SurveyDescription = survey.SurveyQuizSetting?.Description,
            SurveyCode = survey.SurveyQuizSetting?.SurveyCode,
            CreatedAt = studentSurvey.CreatedAt,
            Questions = questionResults
        };
    }

    /// <summary>
    /// Build question results for a survey
    /// </summary>
    /// <param name="survey">Survey collection</param>
    /// <param name="studentSurvey">Student survey collection</param>
    /// <returns>List of survey question results</returns>
    private List<SurveyQuestionDetailResponseEntity> BuildSurveyQuestionResults(QuizCollection survey, StudentQuizCollection studentSurvey)
    {
        var questionResults = new List<SurveyQuestionDetailResponseEntity>();
        
        // Get question results for this survey - including answers and whether student selected them
        foreach (var question in survey.Questions)
        {
            var answerResults = new List<SurveyAnswerDetailResponse>();
            foreach (var answer in question.Answers)
            {
                var selectedByStudent = studentSurvey.StudentQuizAnswers.Any(sa => 
                    sa.QuestionId == question.QuestionId && sa.AnswerId == answer.AnswerId);
                    
                answerResults.Add(new SurveyAnswerDetailResponse
                {
                    AnswerId = answer.AnswerId,
                    SelectedByStudent = selectedByStudent,
                    AnswerText = answer.AnswerText
                });
            }
            
            questionResults.Add(new SurveyQuestionDetailResponseEntity
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Answers = answerResults
            });
        }
        
        return questionResults;
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

    /// <summary>
    /// Calculate student level based on StudentTranscript grades
    /// Get subjects with status Passed or Not Passed, filter by CoreSubjects, calculate average grade
    /// Level calculation based on average grade (0-10 scale):
    /// - Level 1 (Easy): Average grade less than 5.5 (Below Average/Weak)
    /// - Level 2 (Medium): Average grade 5.5 - 7.4 (Average/Good)
    /// - Level 3 (Hard): Average grade 7.5 or higher (Very Good/Excellent)
    /// </summary>
    private async Task<short> CalculateStudentLevelFromTranscriptAsync(Guid studentId, CancellationToken cancellationToken)
    {
        // Get student transcript with Passed and Not Passed status
        var transcriptResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(
            new StudentTranscriptSelectEvent
            {
                StudentId = studentId
            }, cancellationToken);

            var transcripts = transcriptResponse.Message.Response;
            
            // Filter only Passed and Not Passed subjects
            var passedStatus = ConstantEnum.StudentTranscriptStatus.Passed.GetDescription();
            var notPassedStatus = ConstantEnum.StudentTranscriptStatus.NotPassed.GetDescription();
            
            var relevantTranscripts = transcripts
                .Where(t => t.Status == passedStatus || t.Status == notPassedStatus)
                .ToList();

            if (!relevantTranscripts.Any())
            {
                return 1;
            }

            // Get subject codes from transcripts
            var subjectCodes = relevantTranscripts.Select(t => t.SubjectCode).Distinct().ToList();

        // Get core subjects from CourseService
        var coreSubjectsResponse = await _requestCoreSubjectClient.GetResponse<CoreSubjectSelectEventResponse>(
            new CoreSubjectSelectEvent
            {
                SubjectCodes = subjectCodes
            }, cancellationToken);

        var coreSubjectCodes = coreSubjectsResponse.Message.Response
            .Select(cs => cs.SubjectCode)
            .Distinct()
            .ToList();
        
        // Filter transcripts that are core subjects
        var coreTranscripts = relevantTranscripts
            .Where(t => coreSubjectCodes.Contains(t.SubjectCode))
            .ToList();
        // Default to level 1 if student hasn't taken any core subjects
        if (!coreTranscripts.Any())
        {
            return 1;
        }

        // Calculate average grade of core subjects (both Passed and Not Passed)
        // Grade scale: 0-10
        var totalGrade = coreTranscripts.Sum(t => t.Grade);
        var averageGrade = totalGrade / coreTranscripts.Count;

        // Determine level based on average grade
        // Level 1 (Easy): Grade < 5.5 (Below Average/Weak)
        // Level 2 (Medium): Grade 5.5 - 7.4 (Average/Good)  
        // Level 3 (Hard): Grade >= 7.5 (Very Good/Excellent)
        short level;
        if (averageGrade >= 7.5)
        {
            level = 3;
        }
        else if (averageGrade >= 5.5)
        {
            level = 2;
        }
        else
        {
            level = 1;
        }

        return level;
    }
    
    private async Task<StudentSurveyInsertResponse> PrepareStudentLearningProfileForAiAsync(StudentLearningPathInsertContext context, CancellationToken cancellationToken)
    {
        var studentMajorOrientationEvent = new StudentMajorOrientationEvent();

        if (context.InformationResponse.LearningGoalType == (short) ConstantEnum.LearningGoalType.None)
        {
            // Find INTEREST survey from student quiz collections
            var interestSurvey = context.StudentQuizCollections.FirstOrDefault(sq => sq.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST))!;

            // Get selected answer IDs from student's answers
            var selectedAnswerIds = interestSurvey.StudentQuizAnswers
                .Select(a => a.AnswerId)
                .ToHashSet();

            // Prepare questions and student answers for AI analysis
            var interestQuestions = interestSurvey.Quiz.Questions.Select(question => new StudentInterestQuestion
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                StudentAnswers = question.Answers
                    .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                    .Select(a => a.AnswerText)
                    .ToList()
            }).Where(q => q.StudentAnswers.Any()).ToList();

            // Set message to AI service for analysis
            var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
            {
                StudentId = context.CurrentUser.UserId,
                Questions = interestQuestions
            };

            // Send request to AiService and get response
            var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(studentInterestAnalysisEvent, cancellationToken);
            if (!aiAnalysisResponse.Message.Success)
            {
                context.Response.SetMessage(MessageId.E99999);
                return context.Response;
            }
            
            // Set learning goal from AI analysis result
            studentMajorOrientationEvent.LearningGoal = aiAnalysisResponse.Message.Response.LearningGoal;
        }
        else
        {
            studentMajorOrientationEvent.LearningGoal = context.InformationResponse.LearningGoalName;
        }
        
        // Extract frameworks and languages from technologies response
        var frameworks = context.InformationResponse.Technologies
            .Where(x => x.TechnologyType == (short) ConstantEnum.TechnologyType.Framework)
            .Select(x => x.TechnologyName)
            .ToList();

        var languages = context.InformationResponse.Technologies
            .Where(x => x.TechnologyType == (short ) ConstantEnum.TechnologyType.ProgrammingLanguage)
            .Select(x => x.TechnologyName)
            .ToList();

        // Use AI analysis result to determine major orientation
        studentMajorOrientationEvent.Frameworks = frameworks;
        studentMajorOrientationEvent.Languages = languages;
        studentMajorOrientationEvent.IdentityEntity = new BuildingBlocks.Messaging.Events.QuizService.IdentityEntity
        {
            UserId = context.CurrentUser.UserId,
            Email = context.CurrentUser.Email,
        };
        studentMajorOrientationEvent.LimitTime = $"{context.LimitTime} Giờ";
        studentMajorOrientationEvent.LearningPathId = context.LearningPathId;
        studentMajorOrientationEvent.SemesterId = context.InformationResponse.SemesterId;
        studentMajorOrientationEvent.StudentLevel = context.StudentLevel;

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(StudentMajorOrientationEvent),
            Content = JsonSerializer.Serialize(studentMajorOrientationEvent),
            OccurredOnUtc = DateTime.UtcNow,
        };

        await _outboxService.AddAsync(outboxMessage);
        await _unitOfWork.SaveChangesAsync(context.CurrentUser.Email, cancellationToken);

        context.Response.Success = true;
        context.Response.SetMessage(MessageId.I00001, "Chuẩn bị hồ sơ học tập của sinh viên cho AI");
        return context.Response;
    }

}

public class StudentLearningPathInsertContext
{
    public List<StudentQuizCollection> StudentQuizCollections { get; init; } = null!;
    public IdentityEntity CurrentUser { get; init; } = null!;
    public StudentInformationSelectsEventResponseEntity InformationResponse { get; init; } = null!;
    public StudentSurveyInsertResponse Response { get; init; } = null!;
    public Guid LearningPathId { get; init; }
    public int LimitTime { get; init; }
    public short StudentLevel { get; init; }
}
