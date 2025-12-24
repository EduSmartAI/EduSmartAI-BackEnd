using BaseService.Application.Interfaces.IdentityHepers;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.CourseService;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using MassTransit;
using Moq;
using NUnit.Framework;
using QuizService.Application.Applications.StudentSurveys.Commands;
using QuizService.Application.Interfaces;
using QuizService.Domain.WriteModels;
using QuizService.Infrastructure.Implements;

namespace Services.Tests.QuizServiceTests;

[TestFixture]
public class InsertStudentSurveyTests
{
    private Mock<ICommandRepository<StudentQuiz>> _mockStudentQuizRepository = null!;
    private Mock<IQueryRepository<QuizService.Domain.ReadModels.StudentQuizCollection>> _mockStudentQuizQueryRepository = null!;
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IQueryRepository<QuizService.Domain.ReadModels.QuizCollection>> _mockQuizQueryRepository = null!;
    private Mock<IRequestClient<CourseMajorSemesterSelectEvent>> _mockCourseMajorSemesterClient = null!;
    private Mock<IRequestClient<StudentTranscriptSelectEvent>> _mockStudentTranscriptClient = null!;
    private Mock<IRequestClient<StudentInterestSurveyAnalysisEvent>> _mockStudentInterestAnalysisClient = null!;
    private Mock<ICommandRepository<OutboxMessage>> _mockOutboxRepository = null!;
    private Mock<ILearningPathService> _mockLearningPathService = null!;
    private Mock<IRequestClient<SubjectCodeSelectEvent>> _mockSubjectCodeClient = null!;
    private Mock<IRequestClient<InsertLearningPathEvent>> _mockInsertLearningPathClient = null!;
    private Mock<IRequestClient<CoreSubjectSelectEvent>> _mockCoreSubjectClient = null!;
    private StudentSurveyService _studentSurveyService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _majorId = Guid.NewGuid();
    private readonly Guid _semesterId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _mockStudentQuizRepository = new Mock<ICommandRepository<StudentQuiz>>();
        _mockStudentQuizQueryRepository = new Mock<IQueryRepository<QuizService.Domain.ReadModels.StudentQuizCollection>>();
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockQuizQueryRepository = new Mock<IQueryRepository<QuizService.Domain.ReadModels.QuizCollection>>();
        _mockCourseMajorSemesterClient = new Mock<IRequestClient<CourseMajorSemesterSelectEvent>>();
        _mockStudentTranscriptClient = new Mock<IRequestClient<StudentTranscriptSelectEvent>>();
        _mockStudentInterestAnalysisClient = new Mock<IRequestClient<StudentInterestSurveyAnalysisEvent>>();
        _mockOutboxRepository = new Mock<ICommandRepository<OutboxMessage>>();
        _mockLearningPathService = new Mock<ILearningPathService>();
        _mockSubjectCodeClient = new Mock<IRequestClient<SubjectCodeSelectEvent>>();
        _mockInsertLearningPathClient = new Mock<IRequestClient<InsertLearningPathEvent>>();
        _mockCoreSubjectClient = new Mock<IRequestClient<CoreSubjectSelectEvent>>();

        _studentSurveyService = new StudentSurveyService(
            _mockStudentQuizRepository.Object,
            _mockStudentQuizQueryRepository.Object,
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            _mockQuizQueryRepository.Object,
            _mockCourseMajorSemesterClient.Object,
            _mockStudentTranscriptClient.Object,
            _mockOutboxRepository.Object,
            _mockStudentInterestAnalysisClient.Object,
            _mockLearningPathService.Object,
            _mockSubjectCodeClient.Object,
            _mockInsertLearningPathClient.Object,
            _mockCoreSubjectClient.Object
        );
    }

    [Test]
    public async Task InsertStudentSurvey_MissingInterestOrHabit_ShouldReturnFailed()
    {
        // Arrange
        var user = new IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = ConstRole.Student
        };

        var request = new StudentSurveyInsertCommand
        {
            StudentInformation = new StudentInformation
            {
                MajorId = _majorId,
                SemesterId = _semesterId,
                Technologies = new List<Technology>(),
                LearningGoal = new LearningGoal
                {
                    LearningGoalId = Guid.NewGuid(),
                    LearningGoalType = ConstantEnum.LearningGoalType.Frontend,
                    LearningGoalName = "Frontend"
                }
            },
            StudentSurveys = new List<StudentSurveyInsertRequest>
            {
                new StudentSurveyInsertRequest
                {
                    SurveyId = Guid.NewGuid(),
                    SurveyCode = nameof(ConstantEnum.SurveyCode.INTEREST),
                    Answers = new List<StudentQuizAnswerInsertRequest>()
                }
                // Missing HABIT survey
            },
            IsWantToTakeTest = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        // Act
        var result = await _studentSurveyService.InsertStudentSurveyAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("INTEREST và HABIT"));
    }

    [Test]
    public async Task InsertStudentSurvey_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((IdentityEntity?)null);

        var request = new StudentSurveyInsertCommand
        {
            StudentInformation = new StudentInformation
            {
                MajorId = _majorId,
                SemesterId = _semesterId,
                Technologies = new List<Technology>(),
                LearningGoal = new LearningGoal
                {
                    LearningGoalId = Guid.NewGuid(),
                    LearningGoalType = ConstantEnum.LearningGoalType.Frontend,
                    LearningGoalName = "Frontend"
                }
            },
            StudentSurveys = new List<StudentSurveyInsertRequest>(),
            IsWantToTakeTest = true
        };

        // Act
        var result = await _studentSurveyService.InsertStudentSurveyAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
    }
}

