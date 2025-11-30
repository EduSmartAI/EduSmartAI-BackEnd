using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BaseService.Domain.Snapshort;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using QuizService.Application.Applications.Admin.Queries.StudentSurveys;
using QuizService.Application.Applications.LearningPaths;
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
    private readonly IRequestClient<StudentTranscriptSelectEvent> _requestStudentTranscriptClient;
    private readonly IRequestClient<CoreSubjectSelectEvent> _requestCoreSubjectClient;
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILearningPathService _learningPathService;
    private readonly IRequestClient<SubjectCodeSelectEvent> _subjectCodeSelectEventRequestClient;
    private readonly IRequestClient<MajorAndSemesterSelectEvent> _requestMajorAndSemesterSelectEventClient;
    private readonly IRequestClient<AiRecommendImprovementEvent> _requestAiRecommendImprovementEventClient;
    private readonly IRequestClient<InsertLearningPathEvent> _requestInsertLearningPathEventClient;


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
    /// <param name="learningPathService"></param>
    public StudentSurveyService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<QuizCollection> quizQueryRepository,
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient,
        IRequestClient<StudentTranscriptSelectEvent> requestStudentInterestAnalysisClient,
        ICommandRepository<OutboxMessage> outboxService,
        IRequestClient<CoreSubjectSelectEvent> requestCoreSubjectClient,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient1,
        ILearningPathService learningPathService,
        IRequestClient<SubjectCodeSelectEvent> subjectCodeSelectEventRequestClient,
        IRequestClient<MajorAndSemesterSelectEvent> requestMajorAndSemesterSelectEventClient,
        IRequestClient<AiRecommendImprovementEvent> requestAiRecommendImprovementEventClient, IRequestClient<InsertLearningPathEvent> requestInsertLearningPathEventClient)
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
        _learningPathService = learningPathService;
        _subjectCodeSelectEventRequestClient = subjectCodeSelectEventRequestClient;
        _requestMajorAndSemesterSelectEventClient = requestMajorAndSemesterSelectEventClient;
        _requestAiRecommendImprovementEventClient = requestAiRecommendImprovementEventClient;
        _requestInsertLearningPathEventClient = requestInsertLearningPathEventClient;
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

        // Validate request contains both INTEREST and HABIT surveys
        var requiredCodes = new[] { nameof(ConstantEnum.SurveyCode.INTEREST), nameof(ConstantEnum.SurveyCode.HABIT) };

        var presentCodes = request.StudentSurveys
            .Select(s => s.SurveyCode?.ToString() ?? string.Empty)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!requiredCodes.All(rc => presentCodes.Contains(rc)))
        {
            response.SetMessage(MessageId.E00000, "Sinh viên phải làm cả khảo sát INTEREST và HABIT");
            return response;
        }
        
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
            return response;
        }

        if (!request.IsWantToTakeTest && courseInfoResponse.Message.Response.SemesterNumber < 5)
        {
            response.SetMessage(MessageId.E00000, "Chỉ những sinh viên từ học kỳ 5 trở lên mới được phép tạo lộ trình học tập mà không tham gia kiểm tra đánh giá đầu vào.");
            return response;
        }

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
            
            string learningGoalName = request.StudentInformation.LearningGoal.LearningGoalName;
            
            if (request.StudentInformation.LearningGoal.LearningGoalType == (short) ConstantEnum.LearningGoalType.None)
            {
                var interestSurvey = studentQuizCollections.FirstOrDefault(sq => sq.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));
                if (interestSurvey == null)
                {
                    response.SetMessage(MessageId.E00000, "Không tìm thấy bài khảo sát sở thích học tập");
                    return false;
                }

                var selectedAnswerIds = interestSurvey.StudentQuizAnswers
                    .Select(a => a.AnswerId)
                    .ToHashSet();

                var interestQuestions = interestSurvey.Quiz.Questions.Select(question => new StudentInterestQuestion
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    StudentAnswers = question.Answers
                        .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                        .Select(a => a.AnswerText)
                        .ToList()
                }).Where(q => q.StudentAnswers.Any()).ToList();

                var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
                {
                    StudentId = currentUser.UserId,
                    Questions = interestQuestions
                };

                var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(
                    studentInterestAnalysisEvent, 
                    cancellationToken);
                
                if (!aiAnalysisResponse.Message.Success)
                {
                    response.SetMessage(MessageId.E99999);
                    return false;
                }

                learningGoalName = aiAnalysisResponse.Message.Response.LearningGoal;
            }
            
            if (!request.IsWantToTakeTest)
            {
                List<StudentTranscriptSelectEventResponseEntity> studentTranscripts = new();
                // Check OtherQuestionAnswerCodes for course improvement and evaluation requests
                List<CourseImproveContext> courseImporve = new();
                if (request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any())
                {
                    // Check if student wants course improvement (codes 1, 3, 5)
                    if (request.OtherQuestionAnswerCodes.Any())
                    {
                        // Publish event to StudentService to get student transcript
                        var studentTranscriptEvent = new StudentTranscriptSelectEvent
                        {
                            StudentId = currentUser.UserId
                        };
                        
                        var eventResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(studentTranscriptEvent, cancellationToken);
                        studentTranscripts = eventResponse.Message.Response;
                        if (!eventResponse.Message.Success)
                        {
                            response.SetMessage(MessageId.I00000, eventResponse.Message.Message);
                            return false;
                        }
                        
                        // Publish event to CourseService to get all subject codes
                        var subjectCodeEventResponse = await _subjectCodeSelectEventRequestClient.GetResponse<SubjectCodeSelectEventResponse>(new SubjectCodeSelectEvent(), cancellationToken);
                        if (!subjectCodeEventResponse.Message.Success)
                        {
                            response.SetMessage(MessageId.I00000, subjectCodeEventResponse.Message.Message);
                            return false;
                        }
        
                        var subjectCodes = subjectCodeEventResponse.Message.Response;
                        foreach (var questionCode in request.OtherQuestionAnswerCodes)
                        {
                            switch (questionCode)
                            {
                                case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE:
                                    courseImporve.AddRange(
                                        studentTranscripts
                                            .Where(t => t.Grade >= 5 && t.Grade < 7)
                                            .Select(t => new CourseImproveContext
                                            {
                                                SubjectCode = t.SubjectCode,
                                                Level = (short)ConstantEnum.CourseLevel.Beginner,
                                                SubjectPrerequisiteCode = t.Prerequisite
                                            })
                                            .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                                    );
                                    break;

                                case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE:
                                    courseImporve.AddRange(
                                        studentTranscripts
                                            .Where(t => t.Grade >= 7 && t.Grade < 8)
                                            .Select(t => new CourseImproveContext
                                            {
                                                SubjectCode = t.SubjectCode,
                                                Level = (short)ConstantEnum.CourseLevel.Intermidiate,
                                                SubjectPrerequisiteCode = t.Prerequisite
                                            })
                                            .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                                    );
                                    break;

                                case ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE:
                                    courseImporve.AddRange(
                                        studentTranscripts
                                            .Where(t => t.Grade >= 8 && t.Grade < 9)
                                            .Select(t => new CourseImproveContext
                                            {
                                                SubjectCode = t.SubjectCode,
                                                Level = (short)ConstantEnum.CourseLevel.Advanced,
                                                SubjectPrerequisiteCode = t.Prerequisite
                                            })
                                            .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                                    );
                                    break;
                            }
                        }
                        
                    }
                }
                
                var studentLevelResult = await _learningPathService.CalculateStudentLevelFromTranscriptAsync(currentUser.UserId, cancellationToken);
                if (!studentLevelResult.Success)
                {
                    response.MessageId = studentLevelResult.MessageId;
                    response.Message = studentLevelResult.Message;
                    return false;
                }
                
                var learningPathId = Guid.NewGuid();

                var learningPathEvent = new InsertLearningPathEvent
                {
                    LearningPathId = learningPathId,
                    StudentId = currentUser.UserId,
                    CurrentUserEmail = currentUser.Email,
                    PathName = $"Lộ trình {learningGoalName}"
                };
                var learningPathResponse = await _requestInsertLearningPathEventClient.GetResponse<InsertLearningPathEventResponse>(learningPathEvent, cancellationToken);
                if (!learningPathResponse.Message.Success)
                {
                    response.MessageId = learningPathResponse.Message.MessageId;
                    response.Message = learningPathResponse.Message.Message;
                    return false;
                }
                
                // Get Interest and Habit surveys
                var surveyHabit = studentQuizCollections.First(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));
                var selectedAnswerIds = surveyHabit.Quiz.Questions
                    .SelectMany(q => q.Answers)
                    .Select(a => a.AnswerId)
                    .ToList();
           
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
                
                var surveyInterest = studentQuizCollections.FirstOrDefault(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST));

                var majorCodeSelectEventResponse = await _requestMajorAndSemesterSelectEventClient
                    .GetResponse<MajorAndSemesterSelectEventResponse>(new MajorAndSemesterSelectEvent
                    {
                        MajorId = request.StudentInformation.MajorId,
                        SemesterId = request.StudentInformation.SemesterId
                    }, cancellationToken);
                
                var learningPathCreateRequest = new LearningPathCreationContext
                {
                    StudentQuizCollections = studentQuizCollections,
                    CurrentUser = currentUser,
                    InformationResponse = new StudentInformationSelectsEventResponseEntity
                    {
                        SemesterId = request.StudentInformation.SemesterId,
                        LearningGoalName = request.StudentInformation.LearningGoal.LearningGoalName,
                        LearningGoalType = (short) request.StudentInformation.LearningGoal.LearningGoalType,
                        Technologies = request.StudentInformation.Technologies.Select(x => new StudentTechnologySelectsEventResponseEntity
                        {
                            TechnologyName = x.TechnologyName,
                            TechnologyType = x.TechnologyType
                        }).ToList(),
                    },
                    LearningPathId = learningPathId,
                    LimitTime = limitTime,
                    StudentLevel = studentLevelResult.Response.Level,
                    StudentPassedSubjects = studentLevelResult.Response.PassedSubjects,
                    CourseImprove = courseImporve,
                };

                var learningPathInsertResult = await _learningPathService.CreateLearningPathAsync(learningPathCreateRequest, cancellationToken);
                if (!learningPathInsertResult.Success)
                {
                    response.MessageId = learningPathInsertResult.MessageId;
                    response.Message = learningPathInsertResult.Message;
                    return false;
                }
                
                var aiRecommendImprovementEvent = new AiRecommendImprovementEvent
                {
                    CareerGoal = learningGoalName,
                    MajorCode = majorCodeSelectEventResponse.Message.Response.Major!.MajorCode,
                    AbilityMarks = null,
                    SubjectMarks = studentTranscripts
                        .Select(x => new SubjectMarkEvent
                        {
                            SubjectCode = x.SubjectCode,
                            SubjectName = x.SubjectName,
                            Mark = x.Grade
                        })
                        .ToList(),
                    QuizSurveyEvent = new QuizSurveyEvent
                    {
                        QuizInterests = surveyInterest?.StudentQuizAnswers
                            .Select(qa => new QuizInterestEvent
                            {
                                Question = qa.Question?.QuestionText ?? string.Empty,
                                Answer = qa.Answer?.AnswerText ?? string.Empty
                            })
                            .ToList() ?? new List<QuizInterestEvent>(),
                        QuizHabits = surveyHabit.StudentQuizAnswers
                            .Select(qa => new QuizHabitEvent
                            {
                                Question = qa.Question?.QuestionText ?? string.Empty,
                                Answer = qa.Answer?.AnswerText ?? string.Empty
                            })
                            .ToList()
                    },
                    LearningPathId = learningPathId,
                    Email = currentUser.Email,
                };
                
                outboxMessages.Add(new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(AiRecommendImprovementEvent),
                    Content = JsonSerializer.Serialize(aiRecommendImprovementEvent),
                    OccurredOnUtc = DateTime.UtcNow,
                });
                response.Response = learningPathId;
            }
            
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
                StudentQuizAnswers = studentQuiz.StudentQuizAnswers.Select(x =>
                {
                    var question = quizCollection.Questions.FirstOrDefault(q => q.QuestionId == x.QuestionId);
                    var answer = question?.Answers.FirstOrDefault(a => a.AnswerId == x.AnswerId);
                    
                    return new StudentQuizAnswerCollection
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
                        Question = question,
                        Answer = answer
                    };
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
    
    private int GetStudentStudyTime(List<StudentQuizAnswerCollection> studentQuizAnswers)
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
