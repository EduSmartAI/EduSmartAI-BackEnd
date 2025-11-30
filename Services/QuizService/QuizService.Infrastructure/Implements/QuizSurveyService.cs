using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuizSurveyService : IQuizSurveyService
{
    private readonly ICommandRepository<Quiz> _commandRepository;
    private readonly ICommandRepository<SurveyType> _commandSurveyTypeRepository;
    private readonly IQueryRepository<QuizCollection> _queryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<StudentTranscriptSelectEvent> _studentTranscriptSelectEventRequestClient;
    private readonly IRequestClient<SubjectCodeSelectEvent> _subjectCodeSelectEventRequestClient;

    public QuizSurveyService(ICommandRepository<Quiz> commandRepository,
        IQueryRepository<QuizCollection> queryRepository, 
        ICommandRepository<SurveyType> commandSurveyTypeRepository, 
        IIdentityService identityService,
        IUnitOfWork unitOfWork,
        IRequestClient<StudentTranscriptSelectEvent> studentTranscriptSelectEventRequestClient,
        IRequestClient<SubjectCodeSelectEvent> subjectCodeSelectEventRequestClient)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _commandSurveyTypeRepository = commandSurveyTypeRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _studentTranscriptSelectEventRequestClient = studentTranscriptSelectEventRequestClient;
        _subjectCodeSelectEventRequestClient = subjectCodeSelectEventRequestClient;
    }

    /// <summary>
    /// Insert new survey
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SurveyInsertResponse> InsertSurveyAsync(SurveyInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new SurveyInsertResponse { Success = false };
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var userEmail = _identityService.GetCurrentUser()!.Email;

            var surveyTypeExist = await _commandSurveyTypeRepository.FirstOrDefaultAsync(x => x.SurveyCode == request.SurveyCode, cancellationToken: cancellationToken);
            if (surveyTypeExist == null)
            {
                response.SetMessage(MessageId.E00000, "Mã khảo sát không tồn tại");
                return false;
            }

             // Insert new survey
            var survey = new Quiz
            {
                QuizType = (short) ConstantEnum.TestType.Survey, 
                SurveyQuizSetting = new SurveyQuizSetting
                {
                    SurveyTypeId = surveyTypeExist.SurveyTypeId,
                    Title = request.Title,
                    Description = request.Description,
                },
                
                Questions = request.Questions.Select(q => new Question
                {
                    QuestionText = q.QuestionText,
                    QuestionType = (short) q.QuestionType,
                    Answers =  q.Answers.Select(a => new Answer
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                            AnswerRules = a.AnswerRules!.Select(r => new AnswerRule
                            {
                                NumericMin = r.NumericMin,
                                NumericMax = r.NumericMax,
                                Unit = r.Unit.ToString(),
                                MappedField = r.MappedField,
                                Formula = r.Formula
                            }).ToList()
                        }).ToList()
                }).ToList()
            };
            
            // Save to database
            await _commandRepository.AddAsync(survey);
            await _unitOfWork.SaveChangesAsync(userEmail, cancellationToken);
            
            // Store to read model
            _unitOfWork.Store(QuizCollection.FromWriteModel(survey, surveyTypeExist));
            await _unitOfWork.SessionSaveChangesAsync();

            // Remove cache
            await _unitOfWork.CacheRemoveAsync("survey:list");
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm khảo sát mới");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Select survey detail
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<SurveyDetailSelectResponse> SelectSurveyDetailAsync(SurveyDetailSelectQuery request)
    {
        var response = new SurveyDetailSelectResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser()!;
        
        string cacheKey = CacheKey.StudentSurvey(request.SurveyId);
        
        // Publish event to StudentService to get student transcript
        var studentTranscriptEvent = new StudentTranscriptSelectEvent
        {
            StudentId = currentUser.UserId
        };
        
        var eventResponse = await _studentTranscriptSelectEventRequestClient.GetResponse<StudentTranscriptSelectEventResponse>(studentTranscriptEvent);
        var studentTranscript = eventResponse.Message.Response;
        if (!eventResponse.Message.Success)
        {
            response.SetMessage(MessageId.I00000, eventResponse.Message.Message);
            return response;
        }
        if (!studentTranscript.Any() && request.SemesterNumber > 4)
        {
            response.SetMessage(MessageId.I00000, "Sinh viên từ học kỳ 5 trở lên phải có bảng điểm mới có thể tham gia khảo sát.");
            return response;
        }
        
        // Publish event to CourseService to get all subject codes
        var subjectCodeEventResponse = await _subjectCodeSelectEventRequestClient.GetResponse<SubjectCodeSelectEventResponse>(new SubjectCodeSelectEvent());
        if (!subjectCodeEventResponse.Message.Success)
        {
            response.SetMessage(MessageId.I00000, subjectCodeEventResponse.Message.Message);
            return response;
        }
        
        var subjectCodes = subjectCodeEventResponse.Message.Response;
        
        // Get surveys from cache or database
        var pagedResult = await _queryRepository.GetOrSetPagedAsync(
            // Cache key
            cacheKey,
            // If not in cache, get from database
            async () => await _queryRepository.PagedAsync(
                request.PageIndex,
                request.PageSize,
                x => x.QuizId == request.SurveyId && 
                     x.IsActive &&
                     x.QuizType == (short) ConstantEnum.TestType.Survey
            ),
            // Cache duration
            TimeSpan.FromMinutes(10)
        );
        
        // Map to response entity
        var mappedItems = pagedResult.Items.Select(entity => new SurveyDetailSelectResponseEntity
        {
            SurveyId = entity.QuizId,
            Title = entity.SurveyQuizSetting!.Title,
            Description = entity.SurveyQuizSetting.Description,
            SurveyCode = entity.SurveyQuizSetting!.SurveyCode,
            Questions = entity.Questions.Select(q => new QuestionSurveySelects
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Answers = q.Answers.Select(a => new AnswerSurveySelects
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList()
        }).ToList();
        
        // Analyze student transcript to determine other questions based on grade ranges
        var otherQuestions = new List<OtherQuestion>();
        
        if (studentTranscript.Any())
        {
            // Create a set of valid subject codes for filtering
            var validSubjectCodes = new HashSet<string>(subjectCodes.Select(sc => sc.SubjectCode.ToUpper()));
            
            // Filter transcripts to only include valid subjects that are in subjectCodes
            var validTranscripts = studentTranscript
                .Where(t => validSubjectCodes.Contains(t.SubjectCode.ToUpper()))
                .ToList();
            
            // Check for grades in range 5.0 - 6.9 (ask 2 separate questions: course + evaluation)
            bool hasGrade5To7 = validTranscripts.Any(t => t.Grade >= 5.0 && t.Grade < 7.0);
            if (hasGrade5To7)
            {
                otherQuestions.Add(new OtherQuestion
                {
                    OtherQuestionCode = ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE,
                    OtherQuestionText = ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE.GetDescription()
                });
            }
            
            // Check for grades in range 7.0 - 7.9 (ask 2 separate questions: course + evaluation)
            bool hasGrade7To8 = validTranscripts.Any(t => t.Grade >= 7.0 && t.Grade < 8.0);
            if (hasGrade7To8)
            {
                otherQuestions.Add(new OtherQuestion
                {
                    OtherQuestionCode = ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE,
                    OtherQuestionText = ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE.GetDescription()
                });
            }
            
            // Check for grades in range 8.0 - 9.0 (only ask about course, no evaluation)
            bool hasGrade8To9 = validTranscripts.Any(t => t.Grade >= 8.0 && t.Grade <= 9.0);
            if (hasGrade8To9)
            {
                otherQuestions.Add(new OtherQuestion
                {
                    OtherQuestionCode = ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE,
                    OtherQuestionText = ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE.GetDescription()
                });
            }
        }
        
        // Prepare paginated result
        var paginatedResult = new PagedResult<SurveyDetailSelectResponseEntity>
        {
            Items = mappedItems,
            TotalCount = pagedResult.TotalCount,
            PageSize = pagedResult.PageSize,
        };
        
        // True
        response.Success = true;
        response.Response = paginatedResult;
        response.OtherQuestions = otherQuestions.Any() ? otherQuestions : null;
        response.SetMessage(MessageId.I00001, "Lấy danh sách khảo sát");
        return response;
    }

    /// <summary>
    /// Select surveys
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<SurveySelectsResponse> SelectSurveyAsync(SurveySelectsQuery request)
    {
        var response = new SurveySelectsResponse { Success = false };
        
        string cacheKey = "survey:list";
        
        // Get surveys from cache or database
        var quizList = await _queryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                return await _queryRepository.ToListAsync(x => x.IsActive && x.QuizType == (short) ConstantEnum.TestType.Survey);
            },
            TimeSpan.FromMinutes(10)
        );
        if (!quizList.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát");
            return response;
        }
        
        // Map to response entity
        var surveys = quizList.Select(x => new SurveySelectsResponseEntity
        {
            SurveyId = x.QuizId,
            Title = x.SurveyQuizSetting!.Title,
            Description = x.SurveyQuizSetting.Description,
            SurveyCode = x.SurveyQuizSetting!.SurveyCode
        }).ToList();

        // True
        response.Success = true;
        response.Response = surveys;
        response.SetMessage(MessageId.I00001, "Lấy danh sách khảo sát");
        return response;
    }
}