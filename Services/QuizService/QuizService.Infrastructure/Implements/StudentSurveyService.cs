using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
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
using SurveyAnswerDetailResponse = QuizService.Application.Applications.StudentSurveys.Queries.SurveyAnswerDetailResponse;
using AdminQueries = QuizService.Application.Applications.Admin.Queries.StudentSurveys;
using CourseImproveContext = QuizService.Application.Applications.LearningPaths.CourseImproveContext;
using StudentTranscriptContext = QuizService.Application.Applications.LearningPaths.StudentTranscriptContext;
using SubjectMarkContext = QuizService.Application.Applications.LearningPaths.SubjectMarkContext;

namespace QuizService.Infrastructure.Implements;

public class StudentSurveyService : IStudentSurveyService
{
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly IRequestClient<StudentTranscriptSelectEvent> _requestStudentTranscriptClient;
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILearningPathService _learningPathService;
    private readonly IRequestClient<SubjectCodeSelectEvent> _subjectCodeSelectEventRequestClient;
    private readonly IRequestClient<InsertLearningPathEvent> _requestInsertLearningPathEventClient;
    private readonly IRequestClient<CoreSubjectSelectEvent> _requestCoreSubjectClient;
    
    public StudentSurveyService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<QuizCollection> quizQueryRepository,
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient,
        IRequestClient<StudentTranscriptSelectEvent> requestStudentInterestAnalysisClient,
        ICommandRepository<OutboxMessage> outboxService,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient1,
        ILearningPathService learningPathService,
        IRequestClient<SubjectCodeSelectEvent> subjectCodeSelectEventRequestClient,
        IRequestClient<InsertLearningPathEvent> requestInsertLearningPathEventClient,
        IRequestClient<CoreSubjectSelectEvent> requestCoreSubjectClient)
    {
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizQueryRepository = quizQueryRepository;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
        _outboxService = outboxService;
        _requestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient1;
        _requestStudentTranscriptClient = requestStudentInterestAnalysisClient;
        _learningPathService = learningPathService;
        _subjectCodeSelectEventRequestClient = subjectCodeSelectEventRequestClient;
        _requestInsertLearningPathEventClient = requestInsertLearningPathEventClient;
        _requestCoreSubjectClient = requestCoreSubjectClient;
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
        var currentUser = _identityService.GetCurrentUser();

        #region 1. Validation Phase
        
        // 1.1. Validate request contains both INTEREST and HABIT surveys
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
        
        // 1.2. Get and validate course, major, semester info from CourseService
        var majorAndSemesterEventResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(
            new CourseMajorSemesterSelectEvent
            {
                MajorId = request.StudentInformation.MajorId,
                SemesterId = request.StudentInformation.SemesterId,
            }, cancellationToken);

        if (!majorAndSemesterEventResponse.Message.Success)
        {
            response.MessageId = majorAndSemesterEventResponse.Message.MessageId;
            response.Message = majorAndSemesterEventResponse.Message.Message;
            return response;
        }

        // 1.3. Validate semester requirement for skipping test
        if (!request.IsWantToTakeTest && majorAndSemesterEventResponse.Message.Response.SemesterNumber < 5)
        {
            response.SetMessage(MessageId.E00000, "Chỉ những sinh viên từ học kỳ 5 trở lên mới được phép tạo lộ trình học tập mà không tham gia kiểm tra đánh giá đầu vào.");
            return response;
        }

        // 1.4. Validate survey existence
        var surveyExist = await ValidateSurveyExistenceAsync(request, response);
        if (surveyExist == null || !surveyExist.Any()) return response;

        // 1.5. Validate learning goal requirement
        if (!await ValidateLearningGoalAsync(request, surveyExist, response)) return response;
        
        // 1.7. Validate questions and answers
        if (!ValidateQuestionsAndAnswers(request, surveyExist, response)) return response;

        #endregion

        #region 2. Data Preparation Phase
        
        // Flatten all questions and answers for outbox message
        var allQuestions = surveyExist.SelectMany(q => q.Questions).ToList();
        var allAnswers = allQuestions.SelectMany(q => q.Answers).ToList();
        
        #endregion
        
        #region 3. Transaction Execution Phase
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            #region 3.1. Save Student Quiz Answers
            
            var studentQuizzes = BuildStudentQuizzes(request, currentUser!.UserId);
            await _studentQuizCommandRepository.AddRangeAsync(studentQuizzes);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            var outboxMessages = new List<OutboxMessage>();
            var studentQuizCollections = BuildOutboxForSurveyCollection(studentQuizzes, surveyExist, allQuestions, allAnswers, outboxMessages);
            
            #endregion
            
            #region 3.2. Create Student Major Semester Information Event
            
            var majorSemesterInfoInsertEvent = new StudentMajorSemesterInformationEvent
            {
                StudentId = currentUser.UserId,
                MajorId = request.StudentInformation.MajorId,
                SemesterId = request.StudentInformation.SemesterId,
                MajorName = majorAndSemesterEventResponse.Message.Response.MajorName,
                SemesterName = majorAndSemesterEventResponse.Message.Response.SemesterName,
                ProgramingLanguages = request.StudentInformation.Technologies.Select(x => x.TechnologyId).ToList(),
                LearningGoalId = request.StudentInformation.LearningGoal.LearningGoalId,
            };

            outboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentMajorSemesterInformationEvent),
                Content = JsonSerializer.Serialize(majorSemesterInfoInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            });
            
            #endregion
            
            #region 3.3. Determine Learning Goal Name (with AI Analysis if needed)
            
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
            
            #endregion
            
            #region 3.4. Create Learning Path (if not taking test)
            if (!request.IsWantToTakeTest)
            {
                #region 3.4.1. Get Student Transcript and Process Course Improvement Requests
                
                // Get student transcript (always needed for SubjectMarks in AI event)
                var studentTranscriptEvent = new StudentTranscriptSelectEvent
                {
                    StudentId = currentUser.UserId
                };
                
                var transcriptResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(studentTranscriptEvent, cancellationToken);
                List<StudentTranscriptSelectEventResponseEntity> studentTranscripts = transcriptResponse.Message.Response;
                if (!transcriptResponse.Message.Success)
                {
                    response.SetMessage(MessageId.I00000, transcriptResponse.Message.Message);
                    return false;
                }
                
                List<CourseImproveContext> courseImporve = new();
                HashSet<string> subjectCodesForEvaluation = new();
                
                if (request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any())
                {
                    // Get all subject codes
                    var subjectCodeEventResponse = await _subjectCodeSelectEventRequestClient.GetResponse<SubjectCodeSelectEventResponse>(new SubjectCodeSelectEvent(), cancellationToken);
                    if (!subjectCodeEventResponse.Message.Success)
                    {
                        response.SetMessage(MessageId.I00000, subjectCodeEventResponse.Message.Message);
                        return false;
                    }
    
                    var allSubjectCodes = subjectCodeEventResponse.Message.Response;
                    
                    // Process each other question answer code
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
                                        .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
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
                                        .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
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
                                        .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                                );
                                break;

                            case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_EVALUATION:
                                var subjects5To7 = studentTranscripts
                                    .Where(t => t.Grade >= 5 && t.Grade < 7)
                                    .Select(t => t.SubjectCode)
                                    .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code));
                                foreach (var subjectCode in subjects5To7)
                                {
                                    subjectCodesForEvaluation.Add(subjectCode);
                                }
                                break;

                            case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_EVALUATION:
                                var subjects7To8 = studentTranscripts
                                    .Where(t => t.Grade >= 7 && t.Grade < 8)
                                    .Select(t => t.SubjectCode)
                                    .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code));
                                foreach (var subjectCode in subjects7To8)
                                {
                                    subjectCodesForEvaluation.Add(subjectCode);
                                }
                                break;
                        }
                    }
                }
                
                #endregion
                
                #region 3.4.2. Calculate Student Level from Transcript
                
                var studentLevelResult = await _learningPathService.CalculateStudentLevelFromTranscriptAsync(currentUser.UserId, cancellationToken);
                if (!studentLevelResult.Success)
                {
                    response.MessageId = studentLevelResult.MessageId;
                    response.Message = studentLevelResult.Message;
                    return false;
                }
                
                // Get core transcripts for building level reason
                var passedStatus = ConstantEnum.StudentTranscriptStatus.Passed.GetDescription();
                var notPassedStatus = ConstantEnum.StudentTranscriptStatus.NotPassed.GetDescription();

                var relevantTranscripts = studentTranscripts
                    .Where(t => t.Status == passedStatus || t.Status == notPassedStatus)
                    .ToList();

                var subjectCodes = relevantTranscripts.Select(t => t.SubjectCode).Distinct().ToList();

                var coreSubjectsResponse = await _requestCoreSubjectClient.GetResponse<CoreSubjectSelectEventResponse>(
                    new CoreSubjectSelectEvent
                    {
                        SubjectCodes = subjectCodes
                    }, cancellationToken);

                var coreSubjectCodes = coreSubjectsResponse.Message.Response
                    .Select(cs => cs.SubjectCode)
                    .Distinct()
                    .ToList();

                var coreTranscripts = relevantTranscripts
                    .Where(t => coreSubjectCodes.Contains(t.SubjectCode))
                    .ToList();

                var averageGrade = coreTranscripts.Any() 
                    ? coreTranscripts.Sum(t => t.Grade ?? 0) / coreTranscripts.Count 
                    : 0;

                // Build level reason
                var levelReason = BuildLevelReasonForSurvey(
                    studentLevelResult.Response.Level,
                    coreTranscripts,
                    averageGrade);
                
                #endregion
                // Get survey data
                var surveyHabit = studentQuizCollections.First(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));
                var surveyInterest = studentQuizCollections.FirstOrDefault(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST));
                
                // Calculate limit time from habit survey
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
                #endregion
                
                #region 3.4.3. Create Learning Path Entry
                
                var learningPathId = Guid.NewGuid();

                var evaluationAndImprove = request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any()
                    ? string.Join(",", request.OtherQuestionAnswerCodes.Select(c => ((int)c).ToString()))
                    : null;

                var learningPathEvent = new InsertLearningPathEvent
                {
                    LearningPathId = learningPathId,
                    StudentId = currentUser.UserId,
                    CurrentUserEmail = currentUser.Email,
                    PathName = $"Lộ trình {learningGoalName}",
                    Level = studentLevelResult.Response.Level,
                    LevelReason = levelReason,
                    IsSkipTest = true,
                    LimitTime = limitTime,
                    EvaluationAndImprove = evaluationAndImprove,
                    StudentSurveyIds = studentQuizCollections.Select(sq => sq.StudentQuizId).ToList(),
                    StudentTestId = null,
                    PracticeSubmissionIds = null,
                };
                
                var learningPathResponse = await _requestInsertLearningPathEventClient.GetResponse<InsertLearningPathEventResponse>(learningPathEvent, cancellationToken);
                if (!learningPathResponse.Message.Success)
                {
                    response.MessageId = learningPathResponse.Message.MessageId;
                    response.Message = learningPathResponse.Message.Message;
                    return false;
                }
                
                #endregion
                
                #region 3.4.4. Prepare Learning Path Context Data

                
                #region 3.4.5. Create Learning Path with Courses
                
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
                    CourseImprove = courseImporve,
                    SubjectMarks = studentTranscripts
                        .Where(x => 
                            x.Status == ConstantEnum.StudentTranscriptStatus.NotPassed.GetDescription() ||
                            (x.Status == ConstantEnum.StudentTranscriptStatus.Passed.GetDescription() && 
                             subjectCodesForEvaluation.Contains(x.SubjectCode))
                        )
                        .Select(x => new SubjectMarkContext
                        {
                            SubjectCode = x.SubjectCode,
                            SubjectName = x.SubjectName,
                            Mark = x.Grade
                        })
                        .ToList(),
                    AbilityMarks = null,
                    StudentMajor = new StudentMajor
                    {
                        MajorCode = majorAndSemesterEventResponse.Message.Response.MajorCode,
                        MajorName = majorAndSemesterEventResponse.Message.Response.MajorName
                    },
                    StudentTranscripts = studentTranscripts.Select(x => new StudentTranscriptContext
                    {
                        SubjectCode = x.SubjectCode,
                        Status = x.Status,
                        Mark = x.Grade
                    }).ToList(),
                    // Survey thì không có bài test nên null
                    AbilityImprove = null
                };

                var learningPathInsertResult = await _learningPathService.CreateLearningPathAsync(learningPathCreateRequest, cancellationToken);
                if (!learningPathInsertResult.Success)
                {
                    response.MessageId = learningPathInsertResult.MessageId;
                    response.Message = learningPathInsertResult.Message;
                    return false;
                }
                
                #endregion
                response.Response = learningPathId;
            }
            
            #endregion
            
            #region 3.5. Save All Outbox Messages
            
            await _outboxService.AddRangeAsync(outboxMessages);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            #endregion
            
            #region 3.6. Update Cache
            
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

            #endregion
            
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Ghi nhận câu trả lời của sinh viên");
            return true;
        }, cancellationToken);

        #endregion

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
        
        // Get student survey from database
        var studentSurvey = await _studentQuizQueryRepository.FirstOrDefaultAsync(x => 
            x.StudentQuizId == request.StudentQuizId && 
            x.QuizType == (short)ConstantEnum.TestType.Survey &&
            x.IsActive);
        
        if (studentSurvey == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát của sinh viên");
            return response;
        }

        // Get survey info
        var survey = await _quizQueryRepository.FirstOrDefaultAsync(x => x.QuizId == studentSurvey.QuizId);
        if (survey == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin khảo sát");
            return response;
        }

        // Build question results
        var questionResults = new List<SurveyQuestionResultResponseEntity>();

        foreach (var question in survey.Questions.Where(q => q.IsActive).OrderBy(q => q.CreatedAt))
        {
            var studentAnswers = studentSurvey.StudentQuizAnswers
                .Where(sa => sa.QuestionId == question.QuestionId)
                .Select(sa => sa.AnswerId)
                .ToHashSet();

            var answers = question.Answers
                .Where(a => a.IsActive)
                .Select(a => new AdminSurveyAnswerDetailResponse
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.AnswerText,
                    SelectedByStudent = studentAnswers.Contains(a.AnswerId)
                })
                .ToList();

            questionResults.Add(new AdminQueries.SurveyQuestionResultResponseEntity
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Answers = answers
            });
        }

        response.Success = true;
        response.Response = new AdminStudentSurveySelectDetailResponseEntity
        {
            StudentQuizId = studentSurvey.StudentQuizId,
            StudentId = studentSurvey.StudentId,
            StudentName = studentSurvey.Student!.FullName,
            StudentEmail = studentSurvey.Student!.Email,
            SurveyId = studentSurvey.QuizId,
            SurveyTitle = survey.SurveyQuizSetting?.Title ?? "N/A",
            SurveyDescription = survey.SurveyQuizSetting?.Description,
            SurveyCode = survey.SurveyQuizSetting?.SurveyCode ?? "N/A",
            CreatedAt = studentSurvey.CreatedAt,
            QuestionResults = questionResults
        };
        response.SetMessage(MessageId.I00001, "Lấy thông tin chi tiết khảo sát của sinh viên thành công");
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

    /// <summary>
    /// Build detailed level reason in Vietnamese for students who skip the test (survey-only path)
    /// </summary>
    /// <param name="level">Student level (1-3) calculated from transcript</param>
    /// <param name="coreTranscripts">Core subject transcripts used in calculation</param>
    /// <param name="averageGrade">Average grade from core subjects</param>
    /// <returns>Detailed reason string in Vietnamese</returns>
    private string BuildLevelReasonForSurvey(
        short level,
        List<StudentTranscriptSelectEventResponseEntity> coreTranscripts,
        double averageGrade)
    {
        var reason = new System.Text.StringBuilder();
        
        // Header
        reason.AppendLine(" **Đánh giá trình độ từ bảng điểm học tập**");
        reason.AppendLine();
        reason.AppendLine("Vì bạn đã chọn bỏ qua bài kiểm tra đầu vào, hệ thống sẽ đánh giá trình độ của bạn dựa trên kết quả học tập từ bảng điểm.");
        reason.AppendLine();
        
        // Part 1: Core subjects performance
        reason.AppendLine(" **Kết quả các môn học cốt lõi:**");
        reason.AppendLine();
        
        // Group transcripts by grade range for better visualization
        var excellent = coreTranscripts.Where(t => t.Grade >= 8.5).ToList();
        var good = coreTranscripts.Where(t => t.Grade >= 7 && t.Grade < 8.5).ToList();
        var average = coreTranscripts.Where(t => t.Grade >= 5.5 && t.Grade < 7).ToList();
        var needImprovement = coreTranscripts.Where(t => t.Grade < 5.5).ToList();
        
        if (excellent.Any())
        {
            reason.AppendLine($"**Xuất sắc (≥ 8.5 điểm):** {excellent.Count} môn");
            foreach (var subject in excellent.Take(5))
            {
                reason.AppendLine($"  • {subject.SubjectName} ({subject.SubjectCode}): {subject.Grade:F1}/10");
            }
            if (excellent.Count > 5)
                reason.AppendLine($"  ... và {excellent.Count - 5} môn khác");
            reason.AppendLine();
        }
        
        if (good.Any())
        {
            reason.AppendLine($"**Khá tốt (7.0 - 8.4 điểm):** {good.Count} môn");
            foreach (var subject in good.Take(5))
            {
                reason.AppendLine($"  • {subject.SubjectName} ({subject.SubjectCode}): {subject.Grade:F1}/10");
            }
            if (good.Count > 5)
                reason.AppendLine($"  ... và {good.Count - 5} môn khác");
            reason.AppendLine();
        }
        
        if (average.Any())
        {
            reason.AppendLine($"**Trung bình (5.5 - 6.9 điểm):** {average.Count} môn");
            foreach (var subject in average.Take(5))
            {
                reason.AppendLine($"  • {subject.SubjectName} ({subject.SubjectCode}): {subject.Grade:F1}/10");
            }
            if (average.Count > 5)
                reason.AppendLine($"  ... và {average.Count - 5} môn khác");
            reason.AppendLine();
        }
        
        if (needImprovement.Any())
        {
            reason.AppendLine($"**Cần cải thiện (< 5.5 điểm):** {needImprovement.Count} môn");
            foreach (var subject in needImprovement.Take(5))
            {
                reason.AppendLine($"  • {subject.SubjectName} ({subject.SubjectCode}): {subject.Grade:F1}/10");
            }
            if (needImprovement.Count > 5)
                reason.AppendLine($"  ... và {needImprovement.Count - 5} môn khác");
            reason.AppendLine();
        }
        
        // Part 2: Summary statistics
        reason.AppendLine(" **Thống kê tổng quan:**");
        reason.AppendLine();
        reason.AppendLine($"- Tổng số môn cốt lõi đã học: **{coreTranscripts.Count} môn**");
        reason.AppendLine($"- Điểm trung bình: **{averageGrade:F2}/10**");
        
        var passedCount = coreTranscripts.Count(t => t.Status == ConstantEnum.StudentTranscriptStatus.Passed.GetDescription());
        var notPassedCount = coreTranscripts.Count - passedCount;
        
        reason.AppendLine($"- Số môn đạt: **{passedCount} môn**");
        if (notPassedCount > 0)
        {
            reason.AppendLine($"- Số môn chưa đạt: **{notPassedCount} môn**");
        }
        reason.AppendLine();
        
        // Part 3: Level determination explanation
        reason.AppendLine(" **Xác định trình độ:**");
        reason.AppendLine();
        
        string levelCriteria = averageGrade switch
        {
            >= 7.5 => "Điểm trung bình ≥ 7.5 → **Trình độ 3 (Nâng cao)**",
            >= 5.5 => "Điểm trung bình từ 5.5 đến 7.4 → **Trình độ 2 (Trung bình)**",
            _ => "Điểm trung bình < 5.5 → **Trình độ 1 (Cơ bản)**"
        };
        
        reason.AppendLine($"Dựa vào điểm trung bình {averageGrade:F2}/10 của các môn cốt lõi:");
        reason.AppendLine($"→ {levelCriteria}");
        reason.AppendLine();
        
        // Part 5: Important note
        reason.AppendLine("---");
        reason.AppendLine();
        reason.AppendLine(" **Lưu ý quan trọng:**");
        reason.AppendLine();
        reason.AppendLine("- Đánh giá này dựa hoàn toàn trên **kết quả học tập** từ bảng điểm của bạn.");
        reason.AppendLine("- Nếu bạn muốn có đánh giá chính xác hơn về năng lực thực tế, hãy tham gia **bài kiểm tra đầu vào** để hệ thống có thể đánh giá toàn diện hơn.");
        reason.AppendLine("- Lộ trình học tập sẽ được điều chỉnh linh hoạt dựa trên tiến độ học tập của bạn.");
        reason.AppendLine();
        
        return reason.ToString();
    }
}
