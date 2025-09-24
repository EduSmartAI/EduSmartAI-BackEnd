using System.Text.Json;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService.StudentInformationInsertEvents;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class StudentService : IStudentService
{
    private readonly ICommandRepository<Student> _studentRepository;
    private readonly ICommandRepository<StudentTechnology> _studentTechnologyRepository;
    private readonly IQueryRepository<TechnologyCollection> _technologyQueryRepository;
    private readonly ICommandRepository<StudentLearningGoal> _studentLearningGoalRepository;
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly ICommandRepository<StudentOrientation> _studentOrientationRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
    private readonly IQueryRepository<LearningGoalCollection> _learningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQueryRepository"></param>
    /// <param name="studentRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="studentTechnologyRepository"></param>
    /// <param name="studentLearningGoalRepository"></param>
    /// <param name="studentOrientationRepository"></param>
    /// <param name="learningGoalQueryRepository"></param>
    /// <param name="outboxService"></param>
    public StudentService(IQueryRepository<StudentCollection> studentQueryRepository,
        ICommandRepository<Student> studentRepository, IUnitOfWork unitOfWork,
        ICommandRepository<StudentTechnology> studentTechnologyRepository,
        ICommandRepository<StudentLearningGoal> studentLearningGoalRepository,
        ICommandRepository<StudentOrientation> studentOrientationRepository, IQueryRepository<LearningGoalCollection> learningGoalQueryRepository, 
        ICommandRepository<OutboxMessage> outboxService, IQueryRepository<TechnologyCollection> technologyQueryRepository)
    {
        _studentQueryRepository = studentQueryRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _studentTechnologyRepository = studentTechnologyRepository;
        _studentLearningGoalRepository = studentLearningGoalRepository;
        _studentOrientationRepository = studentOrientationRepository;
        _learningGoalQueryRepository = learningGoalQueryRepository;
        _outboxService = outboxService;
        _technologyQueryRepository = technologyQueryRepository;
    }

    /// <summary>
    /// Insert new student
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UserInsertEventResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken = default)
    {
        var response = new UserInsertEventResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new Student
            var student = new Student
            {
                StudentId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            await _studentRepository.AddAsync(student, request.Enail);

            // If OldUserId is not null, delete the old student record
            if (request.OldUserId.HasValue)
            {
                // Check if the old student exists
                var oldStudent =
                    await _studentRepository.FirstOrDefaultAsync(x => x.StudentId == request.OldUserId && x.IsActive,
                        cancellationToken);
                if (oldStudent != null)
                {
                    _studentRepository.Update(oldStudent, request.Enail, true);

                    // Delete the associated StudentCollection if it exists
                    var oldStudentCollection =
                        await _studentQueryRepository.FirstOrDefaultAsync(x =>
                            x.StudentId == request.OldUserId && x.IsActive);
                    if (oldStudentCollection != null)
                    {
                        _unitOfWork.Delete(oldStudentCollection);
                        await _unitOfWork.SessionSaveChangesAsync();
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var now = DateTime.UtcNow;
            // Insert into StudentCollection
            var studentCollection = new StudentCollection
            {
                StudentId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = now,
                CreatedBy = request.Enail,
                UpdatedAt = now,
                UpdatedBy = request.Enail,
                IsActive = true
            };
            _unitOfWork.Store(studentCollection);
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng ký");
            return true;
        }, cancellationToken);
        return response;
    }
    
    /// <summary>
    /// Insert student major, semester, technologies, learning goal, orientations
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInformationMajorSemesterEventResponse> InsertStudentMajorSemesterInformationAsync(StudentMajorSemesterInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentInformationMajorSemesterEventResponse { Success = false };

        // Check student exist
        var studentExist = await _studentRepository
            .FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IsActive, cancellationToken);
        if (studentExist == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin sinh viên");
            return response;
        }

        // Check student collection exist
        var studentCollection = await _studentQueryRepository
            .FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IsActive);
        if (studentCollection == null)
        {
            response.SetMessage(MessageId.E99002);
            return response;
        }

        // Validate Technologies
        var existingTechs = await _technologyQueryRepository
            .ToListAsync(t => request.TechnologyIds.Contains(t.TechnologyId));

        var missingTechIds = request.TechnologyIds.Except(existingTechs.Select(x => x.TechnologyId)).ToList();
        if (missingTechIds.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy Công nghệ phù hợp");
            return response;
        }

        // Validate Goal
        var existingGoal = await _learningGoalQueryRepository
            .FirstOrDefaultAsync(g => request.LearningGoalId == g.GoalId);
        if (existingGoal == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy mục tiêu phù hợp");
            return response;
        }

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Update student info
            studentExist.MajorId = request.MajorId;
            studentExist.SemesterId = request.SemesterId;
            _studentRepository.Update(studentExist);

            // Insert technologies
            var newStudentTechnologies = request.TechnologyIds
                .Select(techId => new StudentTechnology
                {
                    StudentId = request.StudentId,
                    TechnologyId = techId
                }).ToList();

            await _studentTechnologyRepository.AddRangeAsync(newStudentTechnologies);

            // Insert learning goal
            var newStudentLearningGoal = new StudentLearningGoal
            {
                StudentId = request.StudentId,
                GoalId = request.LearningGoalId
            };
            await _studentLearningGoalRepository.AddAsync(newStudentLearningGoal);

            // Insert orientations
            var newStudentOrientations = request.StudentMajorOrientation.MajorInternals
                .Select(m => new StudentOrientation
                {
                    StudentId = request.StudentId,
                    Technology = m.MajorName,
                    ReasonRecommend = m.Reason,
                    RecommendType = (short) ConstantEnum.OrientationRecommendType.Internal
                })
                .Concat(request.StudentMajorOrientation.MajorExternals.Select(m => new StudentOrientation
                {
                    StudentId = request.StudentId,
                    Technology = m.MajorName,
                    ReasonRecommend = m.Reason,
                    RecommendType = (short) ConstantEnum.OrientationRecommendType.External
                }))
                .ToList();
            
            var studentTechnologyCollections = newStudentTechnologies.Select(x => StudentTechnologyCollection.FromWriteModel(x)).ToList();
            var studentLearningGoalCollections = StudentLearningGoalCollection.FromWriteModel(newStudentLearningGoal, learningGoal:existingGoal);
            var studentOrientationCollections = newStudentOrientations.Select(x => StudentOrientationCollection.FromWriteModel(x)).ToList();
            
            await _studentOrientationRepository.AddRangeAsync(newStudentOrientations);

            // Save event to Outbox
            var @event = new StudentInformationUpdatedEvent
            {
                Student = new StudentEvent
                {
                    StudentId = request.StudentId,
                    MajorId = request.MajorId,
                    MajorName = request.MajorName,
                    SemesterId = request.SemesterId,
                    SemesterName = request.SemesterName
                },
                StudentTechnologies = studentTechnologyCollections,            
                StudentLearningGoal = studentLearningGoalCollections,
                StudentOrientations = studentOrientationCollections,
            };
            
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentInformationUpdatedEvent),
                Content = JsonSerializer.Serialize(@event),
                OccurredOnUtc = DateTime.UtcNow,
            };
            
            await _outboxService.AddAsync(outboxMessage);
            
            // Save to Database
            await _unitOfWork.SaveChangesAsync(request.StudentId.ToString(), cancellationToken);

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm thông tin sinh viên");
            return true;
        }, cancellationToken);

        return response;
    }
}