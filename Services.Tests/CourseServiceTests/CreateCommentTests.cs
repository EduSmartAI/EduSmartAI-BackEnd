using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.Comments.CourseComments.Commands.CreateComment;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Moq;
using NUnit.Framework;
using StackExchange.Redis;
using CourseEntity = Course.Domain.Models.Course;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class CreateCommentTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<ICommandRepository<CourseComment>> _mockCommentRepository = null!;
    private Mock<ICommandRepository<CourseStudentEnrollment>> _mockEnrollmentRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IDatabase> _mockCache = null!;
    private CommentService _commentService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _courseId = Guid.NewGuid();
    private readonly string _content = "Test comment content";

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockCommentRepository = new Mock<ICommandRepository<CourseComment>>();
        _mockEnrollmentRepository = new Mock<ICommandRepository<CourseStudentEnrollment>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCache = new Mock<IDatabase>();

        _commentService = new CommentService(
            _mockCache.Object,
            _mockIdentityService.Object,
            _mockUnitOfWork.Object,
            _mockCommentRepository.Object,
            _mockEnrollmentRepository.Object
        );
    }

    [Test]
    public async Task CreateComment_UserNotAuthenticated_ShouldReturnFailed()
    {
        // Arrange
        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((IdentityEntity?)null);

        // Act
        var result = await _commentService.CreateAsync(_courseId, _content);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("User not authenticated"));
    }

    [Test]
    public async Task CreateComment_UserNotEnrolled_ShouldReturnFailed()
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
        _mockEnrollmentRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, object>>[]>()))
            .ReturnsAsync((CourseStudentEnrollment?)null);

        // Act
        var result = await _commentService.CreateAsync(_courseId, _content);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("Bạn chưa tham gia khóa học này"));
    }

    [Test]
    public async Task CreateComment_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Student"
        };

        var enrollment = new CourseStudentEnrollment
        {
            EnrollmentId = Guid.NewGuid(),
            CourseId = _courseId,
            UserId = _userId,
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);
        _mockEnrollmentRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseStudentEnrollment, object>>[]>()))
            .ReturnsAsync(enrollment);

        _mockCommentRepository.Setup(x => x.AddAsync(
            It.IsAny<CourseComment>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            false))
            .ReturnsAsync(1);

        // Act
        var result = await _commentService.CreateAsync(_courseId, _content);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Contains.Substring("Đã tạo bình luận"));
        Assert.That(result.Response, Is.Not.Null);
        Assert.That(result.Response.Content, Is.EqualTo(_content));

        _mockCommentRepository.Verify(x => x.AddAsync(
            It.Is<CourseComment>(c => c.CourseId == _courseId && c.UserId == _userId && c.Content == _content),
            user.Email), Times.Once);
    }
}

