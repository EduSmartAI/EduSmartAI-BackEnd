using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;
using BuildingBlocks.Messaging.Events.QuizService.StudentMajorOrientationEvents;
using MassTransit;
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
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
    private readonly IPublishEndpoint _publishEndpoint;
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
        _requestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient;
        _outboxService = outboxService;
        _publishEndpoint = publishEndpoint;
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

        // Validate if the student has already taken the survey
        if (!await ValidateStudentSurveyStatusAsync(request, currentUser!.UserId, response, cancellationToken))
            return response;

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
            outboxMessages.Add(BuildOutboxForSurveyCollection(studentQuizzes, surveyExist, allQuestions, allAnswers));

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
            
            // Get study time from HABIT survey
            var surveyHabit = surveyExist.First(x => x.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));
            
            // Get student's answers for the habit survey
            var studentSurveyHabit = request.StudentSurveys.First(x => x.SurveyId == surveyHabit.QuizId);

            // Get selected answer IDs
            var selectedAnswerIds = studentSurveyHabit.Answers.Select(a => a.AnswerId).ToList();
            
            // Build StudentQuizAnswerCollection for selected answers
            var studentQuizAnswers = surveyHabit.Questions
                .SelectMany(q => q.Answers)
                .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                .Select(a => new StudentQuizAnswerCollection
                {
                    AnswerId = a.AnswerId,
                    Answer = a
                })
                .ToList();

            int limitTime = GetStudentStudyTime(studentQuizAnswers);

            
            // If LearningGoalType == None → Analyze interest survey
            if (request.StudentInformation.LearningGoal.LearningGoalType == (short)ConstantEnum.LearningGoalType.None)
            {
                if (!await HandleLearningGoalNoneAsync(request, surveyExist, currentUser, courseInfoResponse,
                        outboxMessages, response, limitTime, cancellationToken))
                    return false;
            }
            else
            {
                if (!await HandleLearningGoalOtherAsync(request, currentUser, courseInfoResponse, outboxMessages,
                        response, cancellationToken))
                    return false;
            }

            await _outboxService.AddRangeAsync(outboxMessages);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

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

    private async Task<bool> ValidateStudentSurveyStatusAsync(StudentSurveyInsertCommand request, Guid studentId,
        StudentSurveyInsertResponse response, CancellationToken cancellationToken)
    {
        // Check student has already taken the survey
        var surveyIdsRequest = request.StudentSurveys.Select(s => s.SurveyId).ToList();
        var studentQuizExist = await _studentQuizCommandRepository
            .FirstOrDefaultAsync(x => surveyIdsRequest.Contains(x.QuizId)
                                      && x.StudentId == studentId
                                      && x.IsActive, cancellationToken);
        if (studentQuizExist != null)
        {
            response.SetMessage(MessageId.I00000, "Khảo sát đã được điền");
            return false;
        }

        return true;
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

    private OutboxMessage BuildOutboxForSurveyCollection(List<StudentQuiz> studentQuizzes,
        List<QuizCollection> surveyExist, List<QuestionCollection> allQuestions, List<AnswerCollection> allAnswers)
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
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(StudentQuizCollectionInsertEvent),
            Content = JsonSerializer.Serialize(studentQuizInsertEvent),
            OccurredOnUtc = DateTime.UtcNow,
        };
    }

    private async Task<bool> HandleLearningGoalNoneAsync(StudentSurveyInsertCommand request,
        List<QuizCollection> surveyExist,
        IdentityEntity currentUser,
        Response<CourseMajorSemesterSelectEventResponse> courseInfoResponse,
        List<OutboxMessage> outboxMessages,
        StudentSurveyInsertResponse response,
        int limitTime,
        CancellationToken cancellationToken)
    {
        // Analyze interest survey
        var surveyInterest = surveyExist.First(x => x.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST));
        // Get student's answers for the interest survey
        var studentSurveyInterest = request.StudentSurveys.First(x => x.SurveyId == surveyInterest.QuizId);
        // Get selected answer IDs
        var selectedAnswerIds = studentSurveyInterest.Answers.Select(a => a.AnswerId).ToList();

        // Prepare questions and student answers for AI analysis
        var interestQuestions = surveyInterest.Questions.Select(question => new StudentInterestQuestion
        {
            QuestionId = question.QuestionId,
            QuestionText = question.QuestionText,
            StudentAnswers = question.Answers.Where(a => selectedAnswerIds.Contains(a.AnswerId))
                .Select(a => a.AnswerText).ToList()
        }).Where(q => q.StudentAnswers.Any()).ToList();

        // Set message to AI service for analysis
        var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
        {
            StudentId = currentUser.UserId,
            Questions = interestQuestions
        };

        // Send request to AiService and get response
        // var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(studentInterestAnalysisEvent, cancellationToken);
        // if (!aiAnalysisResponse.Message.Success)
        // {
        //     response.MessageId = aiAnalysisResponse.Message.MessageId;
        //     response.Message = aiAnalysisResponse.Message.Message;
        //     return false;
        // }

        var learningPathId = Guid.NewGuid();

        // Use AI analysis result to determine major orientation
        var studentMajorOrientationEvent = new StudentMajorOrientationEvent
        {
            // LearningGoal = aiAnalysisResponse.Message.Response.LearningGoal,
            LearningGoal = "Thiết kế và phát triển phần mềm với chuyên môn về UI/UX và phân tích dữ liệu",
            Frameworks = request.StudentInformation.Technologies
                .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.Framework)
                .Select(x => x.TechnologyName).ToList(),
            Languages = request.StudentInformation.Technologies
                .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.ProgrammingLanguage)
                .Select(x => x.TechnologyName).ToList(),
            IdentityEntity =
                new BuildingBlocks.Messaging.Events.QuizService.StudentMajorOrientationEvents.IdentityEntity
                {
                    UserId = currentUser.UserId,
                    Email = currentUser.Email,
                },
            LimitTime = $"{limitTime} Giờ",
            LearningPathId = learningPathId,
            SemesterId = request.StudentInformation.SemesterId,
        };

        await _publishEndpoint.Publish(studentMajorOrientationEvent, cancellationToken);
        
        // Add outbox message
        // outboxMessages.Add(new OutboxMessage
        // {
        //     Id = Guid.NewGuid(),
        //     Type = nameof(StudentMajorSemesterInformationEvent),
        //     Content = JsonSerializer.Serialize(studentMajorOrientationEvent),
        //     OccurredOnUtc = DateTime.UtcNow,
        // });

        // Prepare outbox message for StudentMajorSemesterInformationEvent
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

        // True
        response.SetMessage(MessageId.I00001, "Ghi nhận câu trả lời của sinh viên và đang phân tích định hướng nghề nghiệp");
        response.Response = learningPathId;
        response.Success = true;
        return true;
    }

    private async Task<bool> HandleLearningGoalOtherAsync(StudentSurveyInsertCommand request,
        IdentityEntity currentUser, Response<CourseMajorSemesterSelectEventResponse> courseInfoResponse,
        List<OutboxMessage> outboxMessages, StudentSurveyInsertResponse response, CancellationToken cancellationToken)
    {
        // Get learning goal type name
        var learningGoalTypeValue = request.StudentInformation.LearningGoal.LearningGoalType;
        var learningGoalTypeName = Enum.GetName(typeof(ConstantEnum.LearningGoalType), learningGoalTypeValue)!;

        // Determine major orientation based on learning goal and technologies
        var studentMajorOrientationEvent = new StudentMajorOrientationEvent
        {
            LearningGoal = learningGoalTypeName,
            Frameworks = request.StudentInformation.Technologies
                .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.Framework)
                .Select(x => x.TechnologyName).ToList(),
            Languages = request.StudentInformation.Technologies
                .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.ProgrammingLanguage)
                .Select(x => x.TechnologyName).ToList(),
        };

        outboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(StudentMajorOrientationEvent),
            Content = JsonSerializer.Serialize(studentMajorOrientationEvent),
            OccurredOnUtc = DateTime.UtcNow,
        });

        // Prepare outbox message for StudentMajorSemesterInformationEvent
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

        return true;
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
    /// Get student study time from survey
    /// </summary>
    /// <param name="request"></param>
    /// <param name="contextCancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentStudyTimeResponse> GetStudentStudyTimeAsync(StudentStudyTimeRequest request, CancellationToken contextCancellationToken)
    {
        var response = new StudentStudyTimeResponse { Success = false };

        var studentSurvey = await _studentQuizQueryRepository
            .FirstOrDefaultAsync(x => x.StudentId == request.StudentId
                                      && x.QuizType == (short) ConstantEnum.TestType.Survey);

        if (studentSurvey == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát");
            return response;
        }

        var answerRules = studentSurvey.StudentQuizAnswers
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

        // Calculate total study time in minutes
        int totalMinutes = 0;

        // If have hour/day, days/week and months
        if (hourPerDay.HasValue && daysPerWeek.HasValue && months.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * daysPerWeek.Value * 4 * months.Value;
        }
        // If only have hour/week and months (Consider it 4 weeks/month)
        else if (hourPerWeek.HasValue && months.HasValue)
        {
            totalMinutes = hourPerWeek.Value * 60 * 4 * months.Value;
        }
        // If only have hour/day and months (Consider it 7 days/week)
        else if (hourPerDay.HasValue && months.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * 7 * 4 * months.Value;
        }
        // If only have hour/week and days/week (Consider it 4 weeks/month)
        else if (hourPerDay.HasValue && daysPerWeek.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * daysPerWeek.Value * 4;
        }

        response.Success = true;
        response.Response = totalMinutes;
        response.SetMessage(MessageId.I00001, "Lấy thời gian học tập của sinh viên");
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