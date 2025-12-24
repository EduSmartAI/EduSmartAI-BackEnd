using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using Course.Application.DTOs.SyllabusDTO.Subjects;
using Course.Application.Subjects.Commands.CreateSubject;
using Course.Domain.Models;
using Course.Infrastructure.Implements;
using Moq;
using NUnit.Framework;
using CourseEntity = Course.Domain.Models.Course;

namespace Services.Tests.CourseServiceTests;

[TestFixture]
public class CreateSubjectTests
{
    private Mock<IIdentityService> _mockIdentityService = null!;
    private Mock<ICommandRepository<Subject>> _mockSubjectRepository = null!;
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private SubjectService _subjectService = null!;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly string _subjectCode = "CS101";
    private readonly string _subjectName = "Introduction to Computer Science";

    [SetUp]
    public void Setup()
    {
        _mockIdentityService = new Mock<IIdentityService>();
        _mockSubjectRepository = new Mock<ICommandRepository<Subject>>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();

        _subjectService = new SubjectService(
            _mockSubjectRepository.Object,
            _mockUnitOfWork.Object,
            _mockIdentityService.Object
        );
    }

    [Test]
    public async Task CreateSubject_SubjectCodeAlreadyExists_ShouldReturnFailed()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        var existingSubject = new Subject
        {
            SubjectId = Guid.NewGuid(),
            SubjectCode = _subjectCode.ToUpper(),
            SubjectName = "Existing Subject"
        };

        var request = new CreateSubjectCommand(new CreateSubjectDto(
            _subjectCode,
            _subjectName,
            null,
            null
        ));

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((BaseService.Application.Interfaces.IdentityHepers.IdentityEntity?)user);
        _mockSubjectRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Subject, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Subject, object>>[]>()))
            .ReturnsAsync(existingSubject);

        // Act
        var result = await _subjectService.CreateSubjectAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Contains.Substring("đã tồn tại"));
    }

    [Test]
    public async Task CreateSubject_ValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var user = new BaseService.Application.Interfaces.IdentityHepers.IdentityEntity
        {
            UserId = _userId,
            Email = "test@test.com",
            FullName = "Test User",
            RoleName = "Admin"
        };

        var request = new CreateSubjectCommand(new CreateSubjectDto(
            _subjectCode,
            _subjectName,
            "Test description",
            null
        ));

        _mockIdentityService.Setup(x => x.GetCurrentUser()).Returns((BaseService.Application.Interfaces.IdentityHepers.IdentityEntity?)user);
        _mockSubjectRepository.Setup(x => x.FirstOrDefaultAsync(
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Subject, bool>>>(),
            It.IsAny<CancellationToken>(),
            It.IsAny<System.Linq.Expressions.Expression<System.Func<Subject, object>>[]>()))
            .ReturnsAsync((Subject?)null);

        _mockSubjectRepository.Setup(x => x.AddAsync(
            It.IsAny<Subject>(),
            It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        _mockUnitOfWork.Setup(x => x.SaveChangesAsync(
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _subjectService.CreateSubjectAsync(request, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.Response, Is.True);

        _mockSubjectRepository.Verify(x => x.AddAsync(
            It.Is<Subject>(s => s.SubjectCode == _subjectCode.ToUpper() && s.SubjectName == _subjectName),
            user.Email), Times.Once);
    }
}

