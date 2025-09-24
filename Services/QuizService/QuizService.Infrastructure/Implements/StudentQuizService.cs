using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.CourseMajorSemesterSelectEvents;
using BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;
using BuildingBlocks.Messaging.Events.QuizService.StudentTechnologyOrientationEvents;
using MassTransit;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Applications.StudentSurveys.Consumers.StudentQuizCollectionInsertEvents;
using QuizService.Application.Applications.StudentSurveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using MajorExternal = BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents.MajorExternal;
using MajorInternal = BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents.MajorInternal;

namespace QuizService.Infrastructure.Implements;

public class StudentQuizService : IStudentQuizService
{
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly IRequestClient<StudentMajorOrientationEvent> _requestStudentMajorOrientationClient;   
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
    /// <param name="requestStudentMajorOrientationClient"></param>
    public StudentQuizService(ICommandRepository<StudentQuiz> studentQuizCommandRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IQueryRepository<QuizCollection> quizQueryRepository,
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient, 
        IRequestClient<StudentMajorOrientationEvent> requestStudentMajorOrientationClient,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient,
        ICommandRepository<OutboxMessage> outboxService)
    {
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizQueryRepository = quizQueryRepository;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
        _requestStudentMajorOrientationClient = requestStudentMajorOrientationClient;
        _requestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient;
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

        // Check if the quiz has already been taken
        var surveyIdsRequest = request.StudentSurveys.Select(s => s.SurveyId).ToList();
        var surveyExist = await _quizQueryRepository.ToListAsync(x =>
            surveyIdsRequest.Contains(x.QuizId) && x.QuizType == (byte) ConstantEnum.TestType.Survey 
                                                && x.IsActive);
        if (!surveyExist.Any())
        {
            response.SetMessage(MessageId.E00000, "Khảo sát không tồn tại");
            return response;
        }
        
        // If learning goal type is None, check if the survey list contains the INTEREST survey
        if (request.StudentInformation.LearningGoal.LearningGoalType == (short) ConstantEnum.LearningGoalType.None &&
            surveyExist.FirstOrDefault(x => x.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST)) == null)
        {
            response.SetMessage(MessageId.E00000, "Sinh viên chưa có định hướng nghề nghiệp bắt buộc phải làm khảo sát sở thích học tập");
            return response;
        }

        // Get current user
        var currentUser = _identityService.GetCurrentUser();

        // Check student has already taken the survey
        var studentQuizExist = await _studentQuizCommandRepository
            .FirstOrDefaultAsync(x => surveyIdsRequest.Contains(x.QuizId) && x.StudentId == currentUser!.UserId && 
                                      x.IsActive, cancellationToken);
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

        // Validate requested QuestionIds and AnswerIds
        if (requestedQuestionIds.Any(id => !validQuestionIds.Contains(id)) || requestedAnswerIds.Any(id => !validAnswerIds.Contains(id)))
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
                if (!questionAnswerMap.TryGetValue(answer.QuestionId, out var validAnswers) || !validAnswers.Contains(answer.AnswerId))
                {
                    response.SetMessage(MessageId.E00000, "Đáp án không thuộc về câu hỏi trong khảo sát");
                    return response;
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
                    QuizType = (short) ConstantEnum.TestType.Survey,
                    StudentQuizAnswers = studentSurvey.Answers.Select(ans => new StudentQuizAnswer
                    {
                        QuestionId = ans.QuestionId,
                        AnswerId = ans.AnswerId,
                    }).ToList()
                };
                studentQuizzes.Add(newStudentQuiz);
            }

            // Insert into database
            await _studentQuizCommandRepository.AddRangeAsync(studentQuizzes);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
            // Prepare outbox messages
            var outboxMessages = new List<OutboxMessage>();

            // Set value StudentQuizCollection to send message event
            var studentQuizCollections = new List<StudentQuizCollection>();
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
                studentQuizCollections.Add(studentQuizCollection);
            }
            
            // Send message to this service to insert into Marten
            var studentQuizInsertEvent = new StudentQuizCollectionInsertEvent
            {
                StudentQuizzes = studentQuizCollections
            };
            
            outboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentQuizCollectionInsertEvent),
                Content = JsonSerializer.Serialize(studentQuizInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            });
            
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
            
            var learningGoalTypeValue = request.StudentInformation.LearningGoal.LearningGoalType;
            var learningGoalTypeName = Enum.GetName(typeof(ConstantEnum.LearningGoalType), learningGoalTypeValue);

            // If learning goal type is None, set learningGoalTypeName to null
            if (request.StudentInformation.LearningGoal.LearningGoalType == (short) ConstantEnum.LearningGoalType.None)
            {
                var surveyInterest = surveyExist.First(x => x.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST));
                var studentSurveyInterest = request.StudentSurveys.First(x => x.SurveyId == surveyInterest.QuizId);
                var selectedAnswerIds = studentSurveyInterest.Answers.Select(a => a.AnswerId).ToList();
                
                // Prepare questions and student answers for AI analysis
                var interestQuestions = new List<StudentInterestQuestion>();
                foreach (var question in surveyInterest.Questions)
                {
                    var studentAnswersForQuestion = question.Answers
                        .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                        .Select(a => a.AnswerText)
                        .ToList();
                    
                    if (studentAnswersForQuestion.Any())
                    {
                        interestQuestions.Add(new StudentInterestQuestion
                        {
                            QuestionId = question.QuestionId,
                            QuestionText = question.QuestionText,
                            StudentAnswers = studentAnswersForQuestion
                        });
                    }
                }
                
                // Send interest survey data to AIService for career orientation analysis
                var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
                {
                    StudentId = currentUser!.UserId,
                    Questions = interestQuestions
                };
                
                // Get AI analysis response
                var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(studentInterestAnalysisEvent, cancellationToken);
                if (!aiAnalysisResponse.Message.Success)
                {
                    response.MessageId = aiAnalysisResponse.Message.MessageId;
                    response.Message = aiAnalysisResponse.Message.Message;
                    return false;
                }
                
                // Create StudentMajorSemesterInformationEvent with AI analysis results
                var majorSemesterInfoInsertEvent1 = new StudentMajorSemesterInformationEvent
                {
                    StudentId = currentUser!.UserId,
                    MajorId = request.StudentInformation.MajorId,
                    SemesterId = request.StudentInformation.SemesterId,
                    MajorName = messageSelectResponse.Message.Response.MajorName,
                    SemesterName = messageSelectResponse.Message.Response.SemesterName,
                    ProgramingLanguages = request.StudentInformation.Technologies.Select(x => x.TechnologyId).ToList(),
                    LearningGoalId = request.StudentInformation.LearningGoal.LearningGoalId,
                    StudentMajorOrientation = new StudentMajorOrientation
                    {
                        MajorInternals = aiAnalysisResponse.Message.Response.SuggestedMajors.Select(major => new MajorInternal
                        {
                            MajorName = major,
                            Reason = aiAnalysisResponse.Message.Response.AnalysisReason
                        }).ToList(),
                        MajorExternals = aiAnalysisResponse.Message.Response.SuggestedTechnologies.Select(tech => new MajorExternal
                        {
                            MajorName = tech,
                            Reason = aiAnalysisResponse.Message.Response.AnalysisReason
                        }).ToList()
                    }
                };
                
                var outboxMessageInterestAnalysis = new OutboxMessage
                {
                    Id = Guid.NewGuid(),
                    Type = nameof(StudentMajorSemesterInformationEvent),
                    Content = JsonSerializer.Serialize(majorSemesterInfoInsertEvent1),
                    OccurredOnUtc = DateTime.UtcNow,
                };
                outboxMessages.Add(outboxMessageInterestAnalysis);
                
                // Response 
                response.SetMessage(MessageId.I00001, "Ghi nhận câu trả lời của sinh viên và phân tích định hướng nghề nghiệp");
                response.Success = true;
                return true;
            }
            
            // Set StudentMajorOrientationEvent message
            var studentMajorOrientationEvent = new StudentMajorOrientationEvent
            {
                LearningGoal = learningGoalTypeName!,
                
                Frameworks = request.StudentInformation.Technologies
                    .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.Framework)
                    .Select(x => x.TechnologyName).ToList(),

                Languages = request.StudentInformation.Technologies
                    .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.ProgrammingLanguage)
                    .Select(x => x.TechnologyName).ToList(),
            };
            
            // Send message to AiService to get major orientation
            var messageOrientationResponse = await _requestStudentMajorOrientationClient.GetResponse<StudentMajorOrientationEventResponse>(studentMajorOrientationEvent, cancellationToken);
            if (!messageOrientationResponse.Message.Success)
            {
                response.MessageId = messageOrientationResponse.Message.MessageId;
                response.Message = messageOrientationResponse.Message.Message;
                return false;
            }

            // Send message to StudentService to update student information and insert technologies recommendation
            var @majorSemesterInfoInsertEvent = new StudentMajorSemesterInformationEvent
            {
                StudentId = currentUser!.UserId,
                MajorId = request.StudentInformation.MajorId,
                SemesterId = request.StudentInformation.SemesterId,
                MajorName = messageSelectResponse.Message.Response.MajorName,
                SemesterName = messageSelectResponse.Message.Response.SemesterName,
                ProgramingLanguages = request.StudentInformation.Technologies.Select(x => x.TechnologyId).ToList(),
                LearningGoalId = request.StudentInformation.LearningGoal.LearningGoalId,
                StudentMajorOrientation = new StudentMajorOrientation
                {
                    MajorExternals = messageOrientationResponse.Message.Response.MajorExternals.Select(x => new MajorExternal()
                    {
                        MajorName = x.MajorName,
                        Reason = x.Reason
                    }).ToList(),
                    
                    MajorInternals = messageOrientationResponse.Message.Response.MajorInternals.Select(x => new MajorInternal()
                    {
                        MajorName = x.MajorName,
                        Reason = x.Reason
                    }).ToList(),
                }
            };
            
            var outboxMessageSemesterInfoInsertEvent = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentMajorSemesterInformationEvent),
                Content = JsonSerializer.Serialize(@majorSemesterInfoInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
            outboxMessages.Add(outboxMessageSemesterInfoInsertEvent);
            
            await _outboxService.AddRangeAsync(outboxMessages);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
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
}