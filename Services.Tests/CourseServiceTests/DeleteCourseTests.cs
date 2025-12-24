using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.Courses.Commands.DeleteCourse;
using Course.Application.Interfaces.Helpers.Courses;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;
using Services.Tests.Helpers;
using CourseEntity = Course.Domain.Models.Course;
using VMajorSemesterSubjectCourses = Course.Domain.Models.VMajorSemesterSubjectCourses;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class DeleteCourseTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ICommandRepository<CourseEntity>> _mockCourseRepository = null!;
    private Mock<ICourseCache> _mockCourseCache = null!;
    private CourseService _courseService = null!;

    private readonly Guid _courseId = Guid.NewGuid();
    private readonly Guid _teacherId = Guid.NewGuid();

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockCourseRepository = new Mock<ICommandRepository<CourseEntity>>();
        _mockCourseCache = new Mock<ICourseCache>();

        // Mock all required dependencies for CourseService
        var mockTagRepository = new Mock<ICommandRepository<Tag>>();
        var mockCache = new Mock<StackExchange.Redis.IDatabase>();
        var mockQuizCourseClient = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents.QuizCourseInsertEvent>>();
        var mockTeacherNameClient = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation.GetTeacherNamesEvent>>();
        var mockStudentNameClient = new Mock<MassTransit.IRequestClient<BuildingBlocks.Messaging.Events.StudentService.GetStudentInformation.GetStudentNamesEvent>>();
        var mockPublish = new Mock<MassTransit.IPublishEndpoint>();
        var mockModuleQuizRepository = new Mock<ICommandRepository<ModuleQuiz>>();
        var mockLessonQuizRepository = new Mock<ICommandRepository<LessonQuiz>>();
        var mockCourseWishListRepository = new Mock<ICommandRepository<CourseWishlist>>();
        var mockCourseStudentEnrollmentRepository = new Mock<ICommandRepository<CourseStudentEnrollment>>();
        var mockSlugService = new Mock<Course.Application.Interfaces.Helpers.Courses.ISlugService>();
        var mockCacheKeyFactory = new Mock<Course.Application.Interfaces.Helpers.Courses.ICacheKeyFactory>();
        var mockCourseMapper = new Mock<Course.Application.Interfaces.Helpers.Courses.ICourseMapper>();
        var mockQuizGateway = new Mock<Course.Application.Interfaces.Helpers.Courses.IQuizGateway>();
        var mockSemesterRepository = new Mock<ICommandRepository<Semester>>();
        var mockViewCourseRepo = new Mock<ICommandRepository<VMajorSemesterSubjectCourses>>();
        var mockQuizEventFactory = new Mock<Course.Application.Interfaces.Helpers.Courses.IQuizEventFactory>();
        var mockSyllabusSubjectRepository = new Mock<ICommandRepository<SyllabusSubject>>();
        var mockSubjectCommandRepository = new Mock<ICommandRepository<Subject>>();

        _courseService = new CourseService(
            _mockCourseRepository.Object,
            mockTagRepository.Object,
            _mockUnitOfWork.Object,
            mockCache.Object,
            _mockIdentityService.Object,
            mockQuizCourseClient.Object,
            mockTeacherNameClient.Object,
            mockStudentNameClient.Object,
            mockPublish.Object,
            mockModuleQuizRepository.Object,
            mockLessonQuizRepository.Object,
            mockCourseWishListRepository.Object,
            mockCourseStudentEnrollmentRepository.Object,
            mockSlugService.Object,
            mockCacheKeyFactory.Object,
            _mockCourseCache.Object,
            mockCourseMapper.Object,
            mockQuizGateway.Object,
            mockSemesterRepository.Object,
            mockViewCourseRepo.Object,
            mockQuizEventFactory.Object,
            mockCourseWishListRepository.Object,
            mockCourseStudentEnrollmentRepository.Object,
            mockSyllabusSubjectRepository.Object,
            mockSubjectCommandRepository.Object
        );
    }

    [Test]
    public async Task DeleteCourse_CourseNotFound_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _teacherId,
            Email = "teacher@test.com",
            FullName = "Test Teacher",
            RoleName = "Lecturer"
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var emptyCourses = Enumerable.Empty<CourseEntity>().AsQueryable();
        var asyncEmptyCourses = new AsyncEnumerable<CourseEntity>(emptyCourses.Expression);

        _mockCourseRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseEntity, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseEntity, object>>>()))
            .Returns(asyncEmptyCourses);

        // Act
        var result = await _courseService.DeleteCourseAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.MessageId, Is.EqualTo("E11001"));
    }

    [Test]
    public async Task DeleteCourse_UserNotAuthorized_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = Guid.NewGuid(), // Different teacher
            Email = "other@test.com",
            FullName = "Other Teacher",
            RoleName = "Lecturer"
        };

        var course = new CourseEntity
        {
            CourseId = _courseId,
            TeacherId = _teacherId, // Different from current user
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var courses = new[] { course }.AsQueryable();
        var asyncCourses = new AsyncEnumerable<CourseEntity>(courses.Expression);

        _mockCourseRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseEntity, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseEntity, object>>>()))
            .Returns(asyncCourses);

        // Act
        var result = await _courseService.DeleteCourseAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("không có quyền"));
    }

    [Test]
    public async Task DeleteCourse_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _teacherId,
            Email = "teacher@test.com",
            FullName = "Test Teacher",
            RoleName = "Lecturer"
        };

        var course = new CourseEntity
        {
            CourseId = _courseId,
            TeacherId = _teacherId,
            IsActive = true
        };

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns(user);

        var courses = new[] { course }.AsQueryable();
        var asyncCourses = new AsyncEnumerable<CourseEntity>(courses.Expression);

        _mockCourseRepository.Setup(x => x.Find(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<CourseEntity, bool>>>(),
            It.IsAny<bool>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<Func<System.Linq.IQueryable<CourseEntity>, Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<CourseEntity, object>>>()))
            .Returns(asyncCourses);

        _mockCourseRepository.Setup(x => x.Update(
            It.IsAny<CourseEntity>(),
            It.IsAny<string>(),
            It.IsAny<bool>()))
            .Verifiable();

        _mockUnitOfWork.Setup(x => x.BeginTransactionAsync(
            It.IsAny<Func<Task<bool>>>(),
            It.IsAny<CancellationToken>()))
            .Returns<Func<Task<bool>>, CancellationToken>(async (func, ct) => { await func(); });

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<bool>()))
            .ReturnsAsync(1);

        _mockCourseCache.Setup(x => x.ClearGetAllCacheAsync())
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearCourseDetailForGuestCacheAsync())
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearCourseDetailForLectureCacheAsync())
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearCourseDetailForStudentCacheAsync())
            .Returns(Task.CompletedTask);

        _mockCourseCache.Setup(x => x.ClearCourseTagsCacheAsync())
            .Returns(Task.CompletedTask);

        // Act
        var result = await _courseService.DeleteCourseAsync(_courseId, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);
        Assert.That(result.Message, Contains.Substring("Xóa khóa học"));

        _mockCourseRepository.Verify(x => x.Update(
            It.Is<CourseEntity>(c => c.CourseId == _courseId),
            user.Email,
            true), Times.Once);
    }
}

