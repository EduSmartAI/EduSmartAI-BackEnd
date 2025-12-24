using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.LessonNotes.Commands.CreateNote;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class CreateNoteTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<Note>> _mockNoteRepository = null!;
    private Mock<IDatabase> _mockCache = null!;
    private LessonNoteService _lessonNoteService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();
    private readonly int _timeSeconds = 120;
    private readonly string _content = "This is a test note";

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockNoteRepository = new Mock<ICommandRepository<Note>>();
        _mockCache = new Mock<IDatabase>();

        _lessonNoteService = new LessonNoteService(
            _mockUnitOfWork.Object,
            _mockIdentityService.Object,
            _mockNoteRepository.Object,
            _mockCache.Object
        );
    }

    [Test]
    public async Task CreateNote_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((BaseService.Application.Interfaces.IdentityHepers.IdentityEntity?)null);

        // Act
        var result = await _lessonNoteService.CreateAsync(_lessonId, _timeSeconds, _content, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("not authenticated"));
    }

    [Test]
    public async Task CreateNote_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        _mockNoteRepository.Setup(x => x.AddAsync(
            It.IsAny<Note>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        _mockCache.Setup(x => x.KeyDeleteAsync(
            It.IsAny<RedisKey>(),
            It.IsAny<CommandFlags>()))
            .ReturnsAsync(true);

        // Act
        var result = await _lessonNoteService.CreateAsync(_lessonId, _timeSeconds, _content, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);
        Assert.That(result.Message, Contains.Substring("Tạo note"));

        _mockNoteRepository.Verify(x => x.AddAsync(
            It.Is<Note>(n => n.LessonId == _lessonId && 
                             n.UserId == _userId && 
                             n.TimeSeconds == _timeSeconds && 
                             n.Content == _content),
            user.Email), Times.Once);
    }
}

