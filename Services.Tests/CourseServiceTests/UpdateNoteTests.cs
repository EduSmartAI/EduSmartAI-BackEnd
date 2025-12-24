using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.LessonNotes.Commands.UpdateNote;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class UpdateNoteTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<Note>> _mockNoteRepository = null!;
    private Mock<IDatabase> _mockCache = null!;
    private LessonNoteService _lessonNoteService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _noteId = Guid.NewGuid();
    private readonly Guid _lessonId = Guid.NewGuid();
    private readonly string _newContent = "Updated note content";

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
    public async Task UpdateNote_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((BaseService.Application.Interfaces.IdentityHepers.IdentityEntity?)null);

        // Act
        var result = await _lessonNoteService.UpdateAsync(_noteId, _newContent, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("not authenticated"));
    }

    [Test]
    public async Task UpdateNote_NoteNotFound_ShouldReturnFailed()
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

        _mockNoteRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Note, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Note, object>>[]>()))
            .ReturnsAsync((Note?)null);

        // Act
        var result = await _lessonNoteService.UpdateAsync(_noteId, _newContent, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("Note not found"));
    }

    [Test]
    public async Task UpdateNote_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        var existingNote = new Note
        {
            NoteId = _noteId,
            LessonId = _lessonId,
            UserId = _userId,
            TimeSeconds = 120,
            Content = "Old content",
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        _mockNoteRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Note, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Note, object>>[]>()))
            .ReturnsAsync(existingNote);

        _mockNoteRepository.Setup(x => x.Update(
            It.IsAny<Note>(),
            It.IsAny<string>(),
            It.IsAny<bool>()))
            .Verifiable();

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
        var result = await _lessonNoteService.UpdateAsync(_noteId, _newContent, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);
        Assert.That(result.Message, Contains.Substring("Cập nhật note"));

        _mockNoteRepository.Verify(x => x.Update(
            It.Is<Note>(n => n.NoteId == _noteId && n.Content == _newContent),
            user.Email,
            It.IsAny<bool>()), Times.Once);
    }
}

