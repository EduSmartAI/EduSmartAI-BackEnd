using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Students.Commands.Updates;
using StudentService.Application.Applications.Students.Consumers;
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
    private readonly IQueryRepository<StudentTechnologyCollection> _studentTechnologyQueryRepository;
    private readonly ICommandRepository<StudentLearningGoal> _studentLearningGoalRepository;
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
    private readonly IQueryRepository<LearningGoalCollection> _learningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IRequestClient<MajorAndSemesterSelectEvent> _requestClientMajorAndSemesterSelect;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQueryRepository"></param>
    /// <param name="studentRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="studentTechnologyRepository"></param>
    /// <param name="studentLearningGoalRepository"></param>
    /// <param name="learningGoalQueryRepository"></param>
    /// <param name="outboxService"></param>
    /// <param name="identityService"></param>
    /// <param name="requestClientMajorAndSemesterSelect"></param>
    public StudentService(IQueryRepository<StudentCollection> studentQueryRepository,
        ICommandRepository<Student> studentRepository, IUnitOfWork unitOfWork,
        ICommandRepository<StudentTechnology> studentTechnologyRepository,
        ICommandRepository<StudentLearningGoal> studentLearningGoalRepository,
        IQueryRepository<LearningGoalCollection> learningGoalQueryRepository, 
        ICommandRepository<OutboxMessage> outboxService, 
        IQueryRepository<TechnologyCollection> technologyQueryRepository,
        IQueryRepository<StudentTechnologyCollection> studentTechnologyQueryRepository, 
        IIdentityService identityService,
        IRequestClient<MajorAndSemesterSelectEvent> requestClientMajorAndSemesterSelect)
    {
        _studentQueryRepository = studentQueryRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _studentTechnologyRepository = studentTechnologyRepository;
        _studentLearningGoalRepository = studentLearningGoalRepository;
        _learningGoalQueryRepository = learningGoalQueryRepository;
        _outboxService = outboxService;
        _technologyQueryRepository = technologyQueryRepository;
        _studentTechnologyQueryRepository = studentTechnologyQueryRepository;
        _identityService = identityService;
        _requestClientMajorAndSemesterSelect = requestClientMajorAndSemesterSelect;
    }

    /// <summary>
    /// Insert new student
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInsertEventResponse> InsertStudentAsync(StudentInsertCommand request, CancellationToken cancellationToken = default)
    {
        var response = new StudentInsertEventResponse { Success = false };

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
            
            // Check StudentTechnology exist
            List<StudentTechnologyCollection> newStudentTechnologyCollections = new List<StudentTechnologyCollection>();
            var existingStudentTechnologies = await _studentTechnologyRepository
                .Find(st => st.StudentId == request.StudentId && request.TechnologyIds.Contains(st.TechnologyId) && st.IsActive).ToListAsync(cancellationToken: cancellationToken);
            if (!existingStudentTechnologies.Any())
            {
                // Insert technologies
                var newStudentTechnologies = request.TechnologyIds
                    .Select(techId => new StudentTechnology
                    {
                        StudentId = request.StudentId,
                        TechnologyId = techId
                    }).ToList();
                
                await _studentTechnologyRepository.AddRangeAsync(newStudentTechnologies);
                
                // Map to collection with technology info
                newStudentTechnologyCollections.AddRange(newStudentTechnologies.Select(x =>
                {
                    var tech = existingTechs.FirstOrDefault(t => t.TechnologyId == x.TechnologyId);
                    return StudentTechnologyCollection.FromWriteModel(x, tech);
                }).ToList());
            }
           
            StudentLearningGoalCollection newStudentLearningGoalCollection = null;
            var studentLearningGoalExist = await _studentLearningGoalRepository
                .FirstOrDefaultAsync(slg => slg.StudentId == request.StudentId && slg.GoalId == request.LearningGoalId && slg.IsActive, cancellationToken);
            if (studentLearningGoalExist == null)
            {
                // Insert learning goal
                var newStudentLearningGoal = new StudentLearningGoal
                {
                    StudentId = request.StudentId,
                    GoalId = request.LearningGoalId
                };
                await _studentLearningGoalRepository.AddAsync(newStudentLearningGoal);
                newStudentLearningGoalCollection = StudentLearningGoalCollection.FromWriteModel(newStudentLearningGoal, learningGoal:existingGoal);
            }
            
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
                StudentTechnologies = newStudentTechnologyCollections,            
                StudentLearningGoal = newStudentLearningGoalCollection,
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInformationSelectsEventResponse> GetStudentInformationSelectsAsync(StudentInformationSelectsEvent request, CancellationToken cancellationToken = default)
    {
        var response = new StudentInformationSelectsEventResponse { Success = false };
        
        var studentTechnologiesCollections = await _studentTechnologyQueryRepository.ToListAsync(x => x.StudentId == request.StudentId);

        var studentCollection = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IsActive);

        var studentInfo = new StudentInformationSelectsEventResponseEntity
        {
            SemesterId = studentCollection!.SemesterId ?? Guid.Empty,
            LearningGoalName = studentCollection.LearningGoals.Select(x => x.Goal!.GoalName).FirstOrDefault()!,
            LearningGoalType = studentCollection.LearningGoals.Select(x => x.Goal!.LearningGoalType).FirstOrDefault(),
            Technologies = studentTechnologiesCollections.Select(x => new StudentTechnologySelectsEventResponseEntity
            {
                TechnologyName = x.Technology.TechnologyName,
                TechnologyType = x.Technology.TechnologyType,
            }).ToList(),
        };
        
        // Set response
        response.Response = studentInfo;
        response.Success = true;
        response.SetMessage(MessageId.I00001);
        return response;
    }

    /// <summary>
    /// Update student profile
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentProfileUpdateResponse> UpdateStudentProfileAsync(StudentProfileUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentProfileUpdateResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser();
        var studentExist = await _studentRepository.FirstOrDefaultAsync(x => x.StudentId == currentUser!.UserId && x.IsActive, cancellationToken);
        if (studentExist == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin sinh viên");
            return response;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Update basic info
            studentExist.FirstName = request.FirstName ?? studentExist.FirstName;
            studentExist.LastName = request.LastName ?? studentExist.LastName;
            studentExist.DateOfBirth = request.DateOfBirth ?? studentExist.DateOfBirth;
            studentExist.PhoneNumber = request.PhoneNumber ?? studentExist.PhoneNumber;
            studentExist.Gender = request.Gender ?? studentExist.Gender;
            studentExist.AvatarUrl = request.AvatarUrl ?? studentExist.AvatarUrl;
            studentExist.Address = request.Address ?? studentExist.Address;
            studentExist.MajorId = request.MajorId ?? studentExist.MajorId;
            studentExist.Bio = request.Bio ?? studentExist.Bio;
            studentExist.SemesterId = request.SemesterId ?? studentExist.SemesterId;

            _studentRepository.Update(studentExist);

            // Update technologies if provided
            if (request.Technologies != null)
            {
                // Remove old technologies
                studentExist.StudentTechnologies.Clear();
                // Add new technologies
                foreach (var techId in request.Technologies)
                {
                    studentExist.StudentTechnologies.Add(new StudentTechnology
                    {
                        StudentId = studentExist.StudentId,
                        TechnologyId = techId,
                    });
                }
            }

            // Update learning goals if provided
            if (request.LearningGoals != null)
            {
                studentExist.StudentLearningGoals.Clear();
                foreach (var goalId in request.LearningGoals)
                {
                    studentExist.StudentLearningGoals.Add(new StudentLearningGoal
                    {
                        StudentId = studentExist.StudentId,
                        GoalId = goalId,
                    });
                }
            }
            
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);

            var studentCollection = new StudentCollection();
            
            // Publish event to CourseService to get semester name and major name
            if (request.SemesterId != null || request.MajorId != null)
            {
                var responseMajorAndSemester = await _requestClientMajorAndSemesterSelect
                    .GetResponse<MajorAndSemesterSelectEventResponse>(new MajorAndSemesterSelectEvent
                    {
                        MajorId = request.MajorId,
                        SemesterId = request.SemesterId
                    }, cancellationToken);
                var majorName = responseMajorAndSemester.Message.Response.Major?.MajorName;
                var semesterName = responseMajorAndSemester.Message.Response.Semester?.SemesterName;

                if (majorName != null)
                {
                    studentCollection.MajorName = majorName;
                }
                if (semesterName != null)
                {
                    studentCollection.SemesterName = semesterName;
                }
            }
            
            // Map student to student collection
            studentCollection.FirstName = studentExist.FirstName;
            studentCollection.LastName = studentExist.LastName;
            studentCollection.DateOfBirth = studentExist.DateOfBirth;
            studentCollection.PhoneNumber = studentExist.PhoneNumber;
            studentCollection.Gender = studentExist.Gender;
            studentCollection.AvatarUrl = studentExist.AvatarUrl;
            studentCollection.Address = studentExist.Address;
            studentCollection.MajorId = studentExist.MajorId;
            studentCollection.Bio = studentExist.Bio;
            studentCollection.SemesterId = studentExist.SemesterId;
            studentCollection.UpdatedAt = studentExist.UpdatedAt;
            studentCollection.UpdatedBy = studentExist!.UpdatedBy;
            studentCollection.IsActive = studentExist.IsActive;
            
            var @event = new StudentCollectionEvent
            {
                
            };
            
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentCollectionEvent),
                Content = JsonSerializer.Serialize(@event),
                OccurredOnUtc = DateTime.UtcNow,
            };
            await _outboxService.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật thông tin cá nhân");
            return true;
        }, cancellationToken);

        return response;
    }
}