using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using QuizService.Application.Applications.LearningPaths;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Infrastructure.Implements;

public class LearningPathService : ILearningPathService
{
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizCollectionRepository;
    private readonly IRequestClient<StudentInformationSelectsEvent> _requestStudentInformationSelectsClient;
    private readonly IRequestClient<StudentTranscriptSelectEvent> _requestStudentTranscriptClient;
    private readonly IRequestClient<CoreSubjectSelectEvent> _requestCoreSubjectClient;
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
    private readonly ICommandRepository<OutboxMessage> _outboxRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public LearningPathService(
        IQueryRepository<StudentQuizCollection> studentQuizCollectionRepository,
        IRequestClient<StudentInformationSelectsEvent> requestStudentInformationSelectsClient,
        IRequestClient<StudentTranscriptSelectEvent> requestStudentTranscriptClient,
        IRequestClient<CoreSubjectSelectEvent> requestCoreSubjectClient,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient,
        ICommandRepository<OutboxMessage> outboxRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _studentQuizCollectionRepository = studentQuizCollectionRepository;
        _requestStudentInformationSelectsClient = requestStudentInformationSelectsClient;
        _requestStudentTranscriptClient = requestStudentTranscriptClient;
        _requestCoreSubjectClient = requestCoreSubjectClient;
        _requestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient;
        _outboxRepository = outboxRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<InsertLearningPathWithPreviousSurveyAndTranscriptResponse> InsertLearningPathWithPreviousSurveyAndTranscriptAsync(
        InsertLearningPathWithPreviousSurveyAndTranscriptCommand request, 
        CancellationToken cancellationToken)
    {
        var response = new InsertLearningPathWithPreviousSurveyAndTranscriptResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser()!;
        
        var studentId = currentUser.UserId;
        
        var studentSurveys = await _studentQuizCollectionRepository.GetOrSetListAsync(
            CacheKey.StudentSurvey(studentId),
            async () => await _studentQuizCollectionRepository.ToListAsync(
                sq => sq.StudentId == studentId && sq.QuizType == (short) ConstantEnum.TestType.Survey),
            TimeSpan.FromMinutes(10));
        if (!studentSurveys.Any())
        {
            response.SetMessage(MessageId.E00000, "Sinh viên chưa hoàn thành bài khảo sát nào");
            return response;
        }

        var studentInformationSelectsEvent = new StudentInformationSelectsEvent
        {
            StudentId = studentId
        };

        var informationResponse = await _requestStudentInformationSelectsClient.GetResponse<StudentInformationSelectsEventResponse>(
            studentInformationSelectsEvent, 
            cancellationToken);
        if (!informationResponse.Message.Success)
        {
            response.SetMessage(MessageId.E00000, "Không thể lấy thông tin sinh viên");
            return response;
        }

        var studentLevelResult = await CalculateStudentLevelFromTranscriptAsync(studentId, cancellationToken);
        if (!studentLevelResult.Success)
        {
            response.MessageId = studentLevelResult.MessageId;
            response.Message = studentLevelResult.Message;
            return response;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var learningPathId = Guid.NewGuid();
            
            var surveyHabit = studentSurveys.FirstOrDefault(x => 
                x.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));
            if (surveyHabit == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy bài khảo sát thói quen học tập");
                return false;
            }

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

            var context = new LearningPathCreationContext
            {
                StudentQuizCollections = studentSurveys,
                CurrentUser = new IdentityEntity 
                { 
                    UserId = studentId,
                    Email = currentUser.Email,
                    FullName = currentUser.FullName
                },
                InformationResponse = informationResponse.Message.Response,
                LearningPathId = learningPathId,
                LimitTime = limitTime,
                StudentLevel = studentLevelResult.Response.Level,
                StudentPassedSubjects = studentLevelResult.Response.PassedSubjects
            };

            context.InformationResponse.LearningGoalName = request.LearningGoalName;
            context.InformationResponse.LearningGoalType = (short) request.LearningGoalType;
            
            var result = await CreateLearningPathAsync(context, cancellationToken);
            
            if (!result.Success)
            {
                response.MessageId = result.MessageId;
                response.Message = result.Message;
                return false;
            }

            response.Success = true;
            response.Response = new InsertLearningPathWithPreviousSurveyAndTranscriptResponseEntity
            {
                LearningPathId = learningPathId
            };
            response.SetMessage(MessageId.I00001, "Tạo learning path thành công từ khảo sát và bảng điểm");
            return true;
        }, cancellationToken);

        return response;
    }

    public async Task<LearningPathCreationResult> CreateLearningPathAsync(LearningPathCreationContext context, CancellationToken cancellationToken)
    {
        var result = new LearningPathCreationResult { Success = false };
        
        var studentMajorOrientationEvent = new StudentMajorOrientationEvent
        {
            CourseImproves = context.CourseImprove.Select(x =>
                new BuildingBlocks.Messaging.Events.QuizService.CourseImprove
                {
                    SubjectCode = x.SubjectCode,
                    SubjectPrerequisiteCode = x.SubjectPrerequisiteCode,
                    Level = x.Level
                }).ToList()
        };

        var learningGoalType = context.InformationResponse.LearningGoalType;
        
        if (learningGoalType == (short)ConstantEnum.LearningGoalType.None)
        {
            var interestSurvey = context.StudentQuizCollections.FirstOrDefault(sq => 
                sq.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));

            if (interestSurvey == null)
            {
                result.SetMessage(MessageId.E00000, "Không tìm thấy bài khảo sát sở thích học tập");
                return result;
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
                StudentId = context.CurrentUser.UserId,
                Questions = interestQuestions
            };

            var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(
                studentInterestAnalysisEvent, 
                cancellationToken);
                
            if (!aiAnalysisResponse.Message.Success)
            {
                result.SetMessage(MessageId.E99999);
                return result;
            }

            studentMajorOrientationEvent.LearningGoal = aiAnalysisResponse.Message.Response.LearningGoal;
        }
        else
        {
            studentMajorOrientationEvent.LearningGoal = context.InformationResponse.LearningGoalName;
        }

        var frameworks = context.InformationResponse.Technologies
            .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.Framework)
            .Select(x => x.TechnologyName)
            .ToList();

        var languages = context.InformationResponse.Technologies
            .Where(x => x.TechnologyType == (short)ConstantEnum.TechnologyType.ProgrammingLanguage)
            .Select(x => x.TechnologyName)
            .ToList();

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
        studentMajorOrientationEvent.StudentPassedSubjects = context.StudentPassedSubjects;

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(StudentMajorOrientationEvent),
            Content = JsonSerializer.Serialize(studentMajorOrientationEvent),
            OccurredOnUtc = DateTime.UtcNow,
        };

        await _outboxRepository.AddAsync(outboxMessage);
        await _unitOfWork.SaveChangesAsync(context.CurrentUser.Email, cancellationToken);

        result.Success = true;
        result.SetMessage(MessageId.I00001, "Chuẩn bị hồ sơ học tập của sinh viên cho AI");
        return result;
    }

    public async Task<StudentLevelCalculationResult> CalculateStudentLevelFromTranscriptAsync(Guid studentId, CancellationToken cancellationToken)
    {
        var result = new StudentLevelCalculationResult { Success = false };
        
        var transcriptResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(
            new StudentTranscriptSelectEvent
            {
                StudentId = studentId
            }, cancellationToken);

        if (!transcriptResponse.Message.Success)
        {
            result.SetMessage(MessageId.E00000, "Không thể lấy thông tin bảng điểm của sinh viên");
            return result;
        }

        var transcripts = transcriptResponse.Message.Response;

        if (transcripts == null || !transcripts.Any())
        {
            result.SetMessage(MessageId.E00000, "Sinh viên chưa có bảng điểm. Vui lòng cập nhật bảng điểm hoặc chọn làm bài test để xác định trình độ");
            return result;
        }

        var passedStatus = ConstantEnum.StudentTranscriptStatus.Passed.GetDescription();
        var notPassedStatus = ConstantEnum.StudentTranscriptStatus.NotPassed.GetDescription();

        var relevantTranscripts = transcripts
            .Where(t => t.Status == passedStatus || t.Status == notPassedStatus)
            .ToList();

        if (!relevantTranscripts.Any())
        {
            result.SetMessage(MessageId.E00000, "Sinh viên chưa có môn học nào đã hoàn thành (Passed hoặc Not Passed). Vui lòng cập nhật bảng điểm hoặc chọn làm bài test");
            return result;
        }

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

        if (!coreTranscripts.Any())
        {
            result.SetMessage(MessageId.E00000, "Sinh viên chưa có môn học cốt lõi nào đã hoàn thành. Vui lòng cập nhật bảng điểm hoặc chọn làm bài test");
            return result;
        }

        var totalGrade = coreTranscripts.Sum(t => t.Grade);
        var averageGrade = totalGrade / coreTranscripts.Count;

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

        // Get all passed subjects (only Passed status, not Not Passed)
        var passedSubjects = transcripts
            .Where(t => t.Status == passedStatus)
            .Select(t => t.SubjectCode)
            .Distinct()
            .ToList();

        result.Success = true;
        result.Response = new StudentLevelCalculationResultEntity
        {
            Level = level,
            PassedSubjects = passedSubjects
        };
        result.SetMessage(MessageId.I00001, $"Tính toán level thành công từ bảng điểm. Level: {level}, Điểm trung bình: {averageGrade:F2}");
        return result;
    }

    private int GetStudentStudyTime(IEnumerable<StudentQuizAnswerCollection> studentQuizAnswers)
    {
        var answerRules = studentQuizAnswers
            .Where(a => a.Answer?.AnswerRule != null)
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
