using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Domain.WriteModels;
using QuizService.Infrastructure.Implements;
using Moq;
using NUnit.Framework;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Services.Tests.QuizServiceTests;

[TestFixture]
public class InsertQuizCourseTests
{
    private Mock<ICommandRepository<Quiz>> _mockQuizRepository = null!;
    private Mock<ICommandRepository<OutboxMessage>> _mockOutboxRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IIdentityService> _mockIdentityService = null!;
    private QuizCourseService _quizCourseService = null!;

    private readonly string _userEmail = "test@test.com";

    [SetUp]
    public void Setup()
    {
        _mockQuizRepository = new Mock<ICommandRepository<Quiz>>();
        _mockOutboxRepository = new Mock<ICommandRepository<OutboxMessage>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockIdentityService = new Mock<IIdentityService>();

        // Mock other dependencies
        var mockQuizQueryRepository = new Mock<IQueryRepository<QuizService.Domain.ReadModels.QuizCollection>>();
        var mockStudentQuizCommandRepository = new Mock<ICommandRepository<StudentQuiz>>();
        var mockStudentQuizQueryRepository = new Mock<IQueryRepository<QuizService.Domain.ReadModels.StudentQuizCollection>>();
        var mockAnswerCommandRepository = new Mock<ICommandRepository<Answer>>();
        var mockQuestionCommandRepository = new Mock<ICommandRepository<Question>>();
        var mockGetCourseModuleCountClient = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.CourseService.GetCourseModuleCountEvent>>();
        var mockGetSuggestCourseRetakeEvent = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.QuizService.SuggestCourseRetakeEvent>>();

        _quizCourseService = new QuizCourseService(
            _mockQuizRepository.Object,
            mockQuizQueryRepository.Object,
            _mockUnitOfWork.Object,
            _mockOutboxRepository.Object,
            _mockIdentityService.Object,
            mockStudentQuizCommandRepository.Object,
            mockStudentQuizQueryRepository.Object,
            mockAnswerCommandRepository.Object,
            mockQuestionCommandRepository.Object,
            mockGetCourseModuleCountClient.Object,
            mockGetSuggestCourseRetakeEvent.Object
        );
    }

    [Test]
    public async Task InsertQuizCourse_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var request = new QuizCourseInsertCommand
        {
            UserEmail = _userEmail,
            DurationMinutes = 60,
            PassingScorePercentage = 70,
            ShuffleQuestions = true,
            ShowResultsImmediately = false,
            AllowRetake = true,
            Questions = new List<QuizCourseQuestionsInsert>
            {
                new QuizCourseQuestionsInsert
                {
                    QuestionText = "What is 2+2?",
                    QuestionType = 1,
                    Explanation = "Basic math",
                    Answers = new List<QuizCourseAnswersInsert>
                    {
                        new QuizCourseAnswersInsert { AnswerText = "4", IsCorrect = true },
                        new QuizCourseAnswersInsert { AnswerText = "3", IsCorrect = false },
                        new QuizCourseAnswersInsert { AnswerText = "5", IsCorrect = false }
                    }
                }
            }
        };

        _mockQuizRepository.Setup(x => x.AddAsync(
            It.IsAny<Quiz>()))
            .Returns(Task.CompletedTask);

        _mockOutboxRepository.Setup(x => x.AddAsync(
            It.IsAny<OutboxMessage>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(
            It.IsAny<Func<Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>(async (func, ct) => { await func(); });

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _quizCourseService.InsertQuizCourseAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.Not.Null);
        // Note: QuizId is generated in the service, so we just verify it's set
        Assert.That(result.Message, Contains.Substring("Thêm câu hỏi cho khoá học"));

        _mockQuizRepository.Verify(x => x.AddAsync(
            It.Is<Quiz>(q => q.QuizType == (short)TestType.Exam &&
                             q.CourseQuizSetting != null &&
                             q.Questions.Count == 1)), Times.Once);
    }

    [Test]
    public async Task InsertQuizCourse_WithMultipleQuestions_ShouldReturnSuccess()
    {
        // Arrange
        var request = new QuizCourseInsertCommand
        {
            UserEmail = _userEmail,
            DurationMinutes = 90,
            PassingScorePercentage = 80,
            ShuffleQuestions = false,
            ShowResultsImmediately = true,
            AllowRetake = false,
            Questions = new List<QuizCourseQuestionsInsert>
            {
                new QuizCourseQuestionsInsert
                {
                    QuestionText = "Question 1?",
                    QuestionType = 1,
                    Answers = new List<QuizCourseAnswersInsert>
                    {
                        new QuizCourseAnswersInsert { AnswerText = "Answer 1", IsCorrect = true }
                    }
                },
                new QuizCourseQuestionsInsert
                {
                    QuestionText = "Question 2?",
                    QuestionType = 1,
                    Answers = new List<QuizCourseAnswersInsert>
                    {
                        new QuizCourseAnswersInsert { AnswerText = "Answer 2", IsCorrect = true }
                    }
                }
            }
        };

        _mockQuizRepository.Setup(x => x.AddAsync(
            It.IsAny<Quiz>()))
            .Returns(Task.CompletedTask);

        _mockOutboxRepository.Setup(x => x.AddAsync(
            It.IsAny<OutboxMessage>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(
            It.IsAny<Func<Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>(async (func, ct) => { await func(); });

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        // Act
        var result = await _quizCourseService.InsertQuizCourseAsync(request);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.Not.Null);

        _mockQuizRepository.Verify(x => x.AddAsync(
            It.Is<Quiz>(q => q.Questions.Count == 2)), Times.Once);
    }
}

