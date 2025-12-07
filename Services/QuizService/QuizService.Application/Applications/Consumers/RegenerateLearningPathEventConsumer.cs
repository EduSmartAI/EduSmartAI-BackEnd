using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using QuizService.Application.Applications.LearningPaths;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Application.Applications.Consumers;

public class RegenerateLearningPathEventConsumer : IConsumer<RegenerateLearningPathEvent>
{
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizRepository;
    private readonly IQueryRepository<StudentTestCollection> _studentTestRepository;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly ILearningPathService _learningPathService;
    private readonly IUnitOfWork _unitOfWork;

    public RegenerateLearningPathEventConsumer(IQueryRepository<StudentQuizCollection> studentQuizRepository, IQueryRepository<StudentTestCollection> studentTestRepository, IUnitOfWork unitOfWork, ILearningPathService learningPathService, IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient)
    {
        _studentQuizRepository = studentQuizRepository;
        _studentTestRepository = studentTestRepository;
        _unitOfWork = unitOfWork;
        _learningPathService = learningPathService;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
    }

    public async Task Consume(ConsumeContext<RegenerateLearningPathEvent> context)
    {
        var evt = context.Message;
        var response = new RegenerateLearningPathEventResponse { Success = false };
        
        // Publish event to CourseService to Select Major
        var majorAndSemesterEventResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(
            new CourseMajorSemesterSelectEvent
            {
                MajorId = evt.StudentMajorId,
            });

        if (!majorAndSemesterEventResponse.Message.Success)
        {
            response.MessageId = majorAndSemesterEventResponse.Message.MessageId;
            response.Message = majorAndSemesterEventResponse.Message.Message;
            await context.RespondAsync(response);
            return;
        }

        #region Map data to LearningPathCreationContext
        var currentUser = new IdentityEntity
        {
            Email = evt.StudentEmail,
            UserId = evt.StudentId
        };

        var studentMajor = new StudentMajor
        {
            MajorCode = majorAndSemesterEventResponse.Message.Response.MajorCode,
            MajorName = majorAndSemesterEventResponse.Message.Response.MajorName,
        };
        
        var informationResponse = new StudentInformationSelectsEventResponseEntity
        {
            Technologies = evt.Technologies.Select(x => new StudentTechnologySelectsEventResponseEntity
            {
                TechnologyName = x.TechnologyName,
                TechnologyType = x.TechnologyType
            }).ToList(),
            MajorId = evt.StudentMajorId,
            LearningGoalName = evt.LearningGoal.LearningGoalName,
            LearningGoalType = evt.LearningGoal.LearningGoalType,
            SemesterId = evt.SemesterId,
        };
        var studentQuizCollections = await _studentQuizRepository.ToListAsync(x => x.StudentId == evt.StudentId && x.IsActive);
        List<AbilityMarkContext>? abilityMarks = null;
        if (!evt.IsSkipTest)
        {
            
        }

        var courseImproves = evt.CourseImprove?.Select(x => new LearningPaths.CourseImproveContext
        {
            Level = x.Level,
            SubjectCode = x.SubjectCode,
            SubjectPrerequisiteCode = x.SubjectPrerequisiteCode
        }).ToList();
        
        var subjectMarks = evt.SubjectMarks?.Select(x => new LearningPaths.SubjectMarkContext
        {
            SubjectCode = x.SubjectCode,
            SubjectName = x.SubjectName,
            Mark = x.Mark
        }).ToList();
        
        var studentTranscripts = evt.StudentTranscripts?.Select(x => new LearningPaths.StudentTranscriptContext
        {
            SubjectCode = x.SubjectCode,
            Mark = x.Mark,
            Status = x.Status
        }).ToList();
        #endregion
        
        var learningPathCreationContext = new LearningPathCreationContext
        {
            CurrentUser = currentUser,
            LearningPathId = evt.LearningPathId,
            LimitTime = evt.LimitTime,
            StudentLevel = evt.Level,
            StudentMajor = studentMajor,
            InformationResponse = informationResponse,
            StudentQuizCollections = studentQuizCollections,
            AbilityMarks = abilityMarks,
            CourseImprove = courseImproves,
            SubjectMarks = subjectMarks,
            StudentPassedSubjects = evt.StudentPassedSubjects,
            StudentTranscripts = studentTranscripts
        };
        // Generate learning path details
        await _learningPathService.CreateLearningPathAsync(learningPathCreationContext, cancellationToken: CancellationToken.None);
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001);
        await context.RespondAsync(response);
    }
}