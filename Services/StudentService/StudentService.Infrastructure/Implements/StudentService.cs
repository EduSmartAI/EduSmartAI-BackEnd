using System.Globalization;
using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using ExcelDataReader;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Applications.Students.Commands.Updates;
using StudentService.Application.Applications.Students.Consumers;
using StudentService.Application.Applications.Students.Consumers.StudentInformationUpdateds;
using StudentService.Application.Applications.Students.Queries;
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
    private readonly ICommandRepository<StudentTranscript> _studentTranscriptRepository;
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
    private readonly IQueryRepository<LearningGoalCollection> _learningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IRequestClient<MajorAndSemesterSelectEvent> _requestClientMajorAndSemesterSelect;
    private readonly IRequestClient<AvatarUploadEvent> _requestClientAvatarUpload;
    private readonly IRequestClient<SemesterIdSelectsEvent> _requestClientSemesterIdSelects;

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
    /// <param name="technologyQueryRepository"></param>
    /// <param name="studentTechnologyQueryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="requestClientMajorAndSemesterSelect"></param>
    /// <param name="requestClientAvatarUpload"></param>
    /// <param name="studentTranscriptRepository"></param>
    /// <param name="requestClientSemesterIdSelects"></param>
    public StudentService(IQueryRepository<StudentCollection> studentQueryRepository,
        ICommandRepository<Student> studentRepository,
        IUnitOfWork unitOfWork,
        ICommandRepository<StudentTechnology> studentTechnologyRepository,
        ICommandRepository<StudentLearningGoal> studentLearningGoalRepository,
        IQueryRepository<LearningGoalCollection> learningGoalQueryRepository, 
        ICommandRepository<OutboxMessage> outboxService, 
        IQueryRepository<TechnologyCollection> technologyQueryRepository,
        IQueryRepository<StudentTechnologyCollection> studentTechnologyQueryRepository, 
        IIdentityService identityService,
        IRequestClient<MajorAndSemesterSelectEvent> requestClientMajorAndSemesterSelect, 
        IRequestClient<AvatarUploadEvent> requestClientAvatarUpload,
        ICommandRepository<StudentTranscript> studentTranscriptRepository, 
        IRequestClient<SemesterIdSelectsEvent> requestClientSemesterIdSelects)
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
        _requestClientAvatarUpload = requestClientAvatarUpload;
        _studentTranscriptRepository = studentTranscriptRepository;
        _requestClientSemesterIdSelects = requestClientSemesterIdSelects;
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
    /// Insert student major, semester, technologies, learning goal
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
            response.SetMessage(MessageId.E00000, CommonMessages.EStudentNotFound);
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
            await _unitOfWork.SaveChangesAsync(request.StudentId.ToString(), cancellationToken);

            // Get all existing student technologies (active and inactive)
            var allExistingStudentTechnologies = await _studentTechnologyRepository
                .Find(st => st.StudentId == request.StudentId)
                .ToListAsync(cancellationToken: cancellationToken);

            var existingTechIds = allExistingStudentTechnologies
                .Where(st => st is { IsActive: true })
                .Select(st => st!.TechnologyId)
                .ToList();
            
            // Technologies to add (in request but not in database or inactive)
            var techIdsToAdd = request.TechnologyIds.Except(existingTechIds).ToList();
            
            // Technologies to softly delete (in database active but not in request)
            var techIdsToDelete = existingTechIds.Except(request.TechnologyIds).ToList();
            
            // Add new technologies
            if (techIdsToAdd.Any())
            {
                foreach (var techId in techIdsToAdd)
                {
                    // Check if it exists but inactive
                    var inactiveTech = allExistingStudentTechnologies
                        .FirstOrDefault(st => st != null && st.TechnologyId == techId && !st.IsActive);
                    
                    if (inactiveTech != null)
                    {
                        // Reactivate
                        _studentTechnologyRepository.Update(inactiveTech);
                    }
                    else
                    {
                        // Create new
                        await _studentTechnologyRepository.AddAsync(new StudentTechnology
                        {
                            StudentId = request.StudentId,
                            TechnologyId = techId
                        });
                    }
                }
                await _unitOfWork.SaveChangesAsync(request.StudentId.ToString(), cancellationToken);
            }
            
            // Soft delete technologies not in request
            if (techIdsToDelete.Any())
            {
                var techsToDelete = allExistingStudentTechnologies
                    .Where(st => st != null && techIdsToDelete.Contains(st.TechnologyId) && st.IsActive)
                    .ToList();
                
                foreach (var tech in techsToDelete)
                {
                    if (tech != null) _studentTechnologyRepository.Update(tech);
                }
                
                await _unitOfWork.SaveChangesAsync(request.StudentId.ToString(), cancellationToken, needLogicalDelete: true);
            }
            
            // Refresh student technologies after save to get updated active status
            var updatedStudentTechnologies = await _studentTechnologyRepository
                .Find(st => st.StudentId == request.StudentId && request.TechnologyIds.Contains(st.TechnologyId) && st.IsActive)
                .ToListAsync(cancellationToken: cancellationToken);
            
            // Prepare all technologies for event (based on request)
            var studentTechnologyCollections = updatedStudentTechnologies.Select(studentTech =>
            {
                var tech = existingTechs.FirstOrDefault(t => t.TechnologyId == studentTech.TechnologyId);
                return StudentTechnologyCollection.FromWriteModel(studentTech!, tech);
            }).ToList();
            
            // Get all existing learning goals (including inactive ones)
            var allExistingLearningGoals = await _studentLearningGoalRepository
                .Find(slg => slg.StudentId == request.StudentId)
                .ToListAsync(cancellationToken: cancellationToken);
            
            // Check if the requested learning goal exists
            var studentLearningGoalExist = allExistingLearningGoals
                .FirstOrDefault(slg => slg.GoalId == request.LearningGoalId);
            
            if (studentLearningGoalExist == null)
            {
                // Insert new learning goal if it doesn't exist
                studentLearningGoalExist = new StudentLearningGoal
                {
                    StudentId = request.StudentId,
                    GoalId = request.LearningGoalId
                };
                await _studentLearningGoalRepository.AddAsync(studentLearningGoalExist);
            }
            else if (!studentLearningGoalExist.IsActive)
            {
                // Reactivate if it exists but is inactive
                _studentLearningGoalRepository.Update(studentLearningGoalExist);
            }
            
            await _unitOfWork.SaveChangesAsync(request.StudentId.ToString(), cancellationToken);
            
            // Prepare learning goal for event
            var studentLearningGoalCollection = StudentLearningGoalCollection.FromWriteModel(studentLearningGoalExist, learningGoal: existingGoal);
            
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
                StudentLearningGoal = studentLearningGoalCollection,
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
        var studentExist = await _studentRepository
            .FirstOrDefaultAsync(predicate: x => x.StudentId == currentUser!.UserId && x.IsActive,
                cancellationToken: cancellationToken,
                x => x.StudentTechnologies,
                              x => x.StudentLearningGoals);
        if (studentExist == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin sinh viên");
            return response;
        }
        
        // Check technologies exist
        var technologiesExist = await _technologyQueryRepository.ToListAsync(t => request.Technologies != null && request.Technologies.Contains(t.TechnologyId));
        var missingTechIds = request.Technologies?.Except(technologiesExist.Select(x => x.TechnologyId)).ToList();
        if (missingTechIds != null && missingTechIds.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy Công nghệ phù hợp");
            return response;
        }
        
        // Check learning goals exist
        var learningGoalsExist = await _learningGoalQueryRepository.ToListAsync(lg => request.LearningGoals != null && request.LearningGoals.Contains(lg.GoalId));
        var missingGoalIds = request.LearningGoals?.Except(learningGoalsExist.Select(x => x.GoalId)).ToList();
        if (missingGoalIds != null && missingGoalIds.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy Mục tiêu học tập phù hợp");
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
            studentExist.Address = request.Address ?? studentExist.Address;
            studentExist.MajorId = request.MajorId ?? studentExist.MajorId;
            studentExist.Bio = request.Bio ?? studentExist.Bio;
            studentExist.SemesterId = request.SemesterId ?? studentExist.SemesterId;

            if (request.Avatar != null)
            {
                await using var ms = new MemoryStream();
                await request.Avatar.CopyToAsync(ms, cancellationToken);
                var fileBytes = ms.ToArray();
                
                // Publish event to UtilityService to upload avatar
                var avatarUploadEvent = new AvatarUploadEvent
                {
                    FileName = request.Avatar.FileName,
                    ContentType = request.Avatar.ContentType,
                    FileData = fileBytes
                };
                
                var avatarUrlResponse = await _requestClientAvatarUpload.GetResponse<AvatarUploadEventResponse>(avatarUploadEvent, cancellationToken);
                if (!avatarUrlResponse.Message.Success)
                {
                    response.SetMessage(MessageId.E00000, "Có lỗi xảy ra trong quá trình tải ảnh đại diện");
                    return false;
                }
                studentExist.AvatarUrl = avatarUrlResponse.Message.Response.AvatarUrl;
            }

            _studentRepository.Update(studentExist);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);

            // Update technologies if provided
            if (request.Technologies != null)
            {
                var existingTechs = studentExist.StudentTechnologies.ToList();

                foreach (var techId in request.Technologies)
                {
                    var existingTech = existingTechs.FirstOrDefault(t => t.TechnologyId == techId);
                    if (existingTech != null)
                    {
                        // If already exists but inactive, activate it
                        if (!existingTech.IsActive)
                        {
                            _studentTechnologyRepository.Update(existingTech);
                        }
                    }
                    else
                    {
                        var newStudentTech = new StudentTechnology
                        {
                            StudentId = studentExist.StudentId,
                            TechnologyId = techId,
                        };
                        // If not exists, add new
                        studentExist.StudentTechnologies.Add(newStudentTech);
                        await _studentTechnologyRepository.AddAsync(newStudentTech);
                    }
                }

                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

                // If any existing technologies are not in the new list, mark them as inactive
                var inactiveTechs = existingTechs
                    .Where(t => !request.Technologies.Contains(t.TechnologyId))
                    .ToList();

                foreach (var tech in inactiveTechs)
                {
                    _studentTechnologyRepository.Update(tech);
                }

                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);
            }
            else
            {
                // Delete all technologies if null
                var inactiveTechs = studentExist.StudentTechnologies.ToList();
                foreach (var tech in inactiveTechs)
                {
                    _studentTechnologyRepository.Update(tech);
                }
                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);

            }

            // Update learning goals if provided
            if (request.LearningGoals != null)
            {
                var existingGoals = studentExist.StudentLearningGoals.ToList();

                foreach (var goalId in request.LearningGoals)
                {
                    var existingGoal = existingGoals.FirstOrDefault(g => g.GoalId == goalId);
                    if (existingGoal != null)
                    {
                        // If already exists but inactive, activate it
                        if (!existingGoal.IsActive)
                        {
                            _studentLearningGoalRepository.Update(existingGoal);
                        }
                    }
                    else
                    {
                        var newLearningGoal = new StudentLearningGoal
                        {
                            StudentId = studentExist.StudentId,
                            GoalId = goalId,
                        };
                        // If not exists, add new
                        studentExist.StudentLearningGoals.Add(newLearningGoal);
                        await _studentLearningGoalRepository.AddAsync(newLearningGoal);
                    }
                }
                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

                // If any existing goals are not in the new list, mark them as inactive
                var inactiveGoals = existingGoals
                    .Where(g => !request.LearningGoals.Contains(g.GoalId))
                    .ToList();

                foreach (var goal in inactiveGoals)
                {
                    _studentLearningGoalRepository.Update(goal);
                }
                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);
            } 
            else
            {
                // Delete all learning goals if null
                var inactiveGoals = studentExist.StudentLearningGoals.ToList();
                foreach (var goal in inactiveGoals)
                {
                    _studentLearningGoalRepository.Update(goal);
                }
                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);
            }
            
            // Get updated student with latest technologies and learning goals
            var updatedStudent = await _studentRepository
                .FirstOrDefaultAsync(predicate: x => x.StudentId == currentUser.UserId && x.IsActive,
                    cancellationToken: cancellationToken,
                    x => x.StudentTechnologies,
                    x => x.StudentLearningGoals);
            
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
            studentCollection = StudentCollection.FromWriteModel(studentExist);
            
            // Map Technologies to StudentTechnologyCollection
            studentCollection.Technologies = updatedStudent!.StudentTechnologies
                .Where(st => st.IsActive)
                .Select(st =>
                {
                    var tech = technologiesExist.FirstOrDefault(t => t.TechnologyId == st.TechnologyId);
                    return StudentTechnologyCollection.FromWriteModel(st, tech);
                }).ToList();
            
            // Map LearningGoals to StudentLearningGoalCollection
            studentCollection.LearningGoals = updatedStudent.StudentLearningGoals
                .Where(slg => slg.IsActive)
                .Select(slg =>
                {
                    var goal = learningGoalsExist.FirstOrDefault(lg => lg.GoalId == slg.GoalId);
                    return StudentLearningGoalCollection.FromWriteModel(slg, goal);
                }).ToList();
            
            // Publish event to store collection
            var studentCollectionEvent = new StudentCollectionEvent
            {
                Student = studentCollection
            };
            
            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentCollectionEvent),
                Content = JsonSerializer.Serialize(studentCollectionEvent),
                OccurredOnUtc =  DateTime.UtcNow,
            };

            await _unitOfWork.CacheRemoveAsync(CacheKey.StudentProfile(currentUser.UserId));
            await _outboxService.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật thông tin cá nhân");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Select student profile
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentProfileSelectResponse> SelectStudentProfileAsync(StudentProfileSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new StudentProfileSelectResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser();
        if (currentUser == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin người dùng");
            return response;
        }

        var cacheKey = CacheKey.StudentProfile(currentUser.UserId);

        // Get student collection with all related data
        var studentCollection = await _studentQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                return await _studentQueryRepository.FirstOrDefaultAsync(x =>
                    x.StudentId == currentUser.UserId && x.IsActive);
            },
            TimeSpan.FromMinutes(5));
        if (studentCollection == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin sinh viên");
            return response;
        }
        
        // Map to response
        var studentProfile = new StudentProfileSelectResponseEntity
        {
            StudentId = studentCollection.StudentId,
            FirstName = studentCollection.FirstName!,
            LastName = studentCollection.LastName!,
            DateOfBirth = studentCollection.DateOfBirth,
            PhoneNumber = studentCollection.PhoneNumber,
            Gender = studentCollection.Gender,
            AvatarUrl = studentCollection.AvatarUrl,
            Address = studentCollection.Address,
            Bio = studentCollection.Bio,
            MajorId = studentCollection.MajorId,
            MajorName = studentCollection.MajorName,
            SemesterId = studentCollection.SemesterId,
            SemesterName = studentCollection.SemesterName,
            Technologies = studentCollection.Technologies?.Select(t => new StudentTechnologyItem
            {
                TechnologyId = t.TechnologyId,
                TechnologyName = t.Technology.TechnologyName,
                TechnologyType = t.Technology.TechnologyType,
                TechnologyTypeName = GetTechnologyTypeName(t.Technology.TechnologyType)
            }).ToList() ?? new List<StudentTechnologyItem>(),
            LearningGoals = studentCollection.LearningGoals?.Select(lg => new StudentLearningGoalItem
            {
                GoalId = lg.GoalId,
                GoalName = lg.Goal!.GoalName,
                LearningGoalType = lg.Goal.LearningGoalType,
                LearningGoalTypeName = GetLearningGoalTypeName(lg.Goal.LearningGoalType)
            }).ToList() ?? new List<StudentLearningGoalItem>()
        };

        response.Success = true;
        response.Response = studentProfile;
        response.SetMessage(MessageId.I00001, "Lấy thông tin cá nhân sinh viên");
        return response;
    }

    /// <summary>
    /// Insert student transcript from Excel file
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentTranscriptInsertResponse> InsertStudentTranscriptAsync(StudentTranscriptInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentTranscriptInsertResponse { Success = false };

        if (request.TranscriptFile.Length == 0)
        {
            response.SetMessage(MessageId.I00000, "File bảng điểm trống. Vui lòng chọn file hợp lệ");
            return response;
        }
        
        var currentUser = _identityService.GetCurrentUser()!;

        // Check if student has existing transcripts, delete them
        var currentTranscripts = await _studentTranscriptRepository
            .Find(st => st.StudentId == currentUser.UserId && st.IsActive)
            .ToListAsync(cancellationToken: cancellationToken);
        if (currentTranscripts.Any())
        {
            _studentTranscriptRepository.UpdateRange(currentTranscripts!);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);
        }
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            
            try
            {
                await using var stream = request.TranscriptFile.OpenReadStream();
                using var reader = ExcelReaderFactory.CreateReader(stream);
            
                var result = reader.AsDataSet();
            
                var table = result.Tables[0];
                
                // Validate minimum rows (header + at least 1 data row)
                if (table.Rows.Count < 2)
                {
                    response.SetMessage(MessageId.E00000, "File bảng điểm không có dữ liệu hoặc thiếu header");
                    return false;
                }
            
                // Validate minimum columns (should have at least 10 columns based on row[9])
                if (table.Columns.Count < 10)
                {
                    response.SetMessage(MessageId.E00000, "File bảng điểm không đúng định dạng. Thiếu các cột bắt buộc");
                    return false;
                }
            
                var studentTranscripts = new List<StudentTranscript>();
            
                var semesterIdSelectsEvent = new SemesterIdSelectsEvent
                {
                    SemesterNumbers = new List<int>()
                };
                
                // Get student info to check semester
                var studentSelect = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == currentUser.UserId);
                int? studentCurrentSemesterNumber = null;
                if (studentSelect?.SemesterId != null)
                {
                    var responseMajorAndSemester = await _requestClientMajorAndSemesterSelect
                        .GetResponse<MajorAndSemesterSelectEventResponse>(new MajorAndSemesterSelectEvent
                        {
                            SemesterId = studentSelect.SemesterId
                        }, cancellationToken);
                    studentCurrentSemesterNumber = responseMajorAndSemester.Message.Response.Semester?.SemesterNumber;
                }

                var validSemesterNumbers = new List<(int key, string value)>();
                for (int i = 1; i < table.Rows.Count; i++)
                {
                    var row = table.Rows[i];
            
                    // Ignore blank lines or comment lines
                    if (row.ItemArray.All(cell => string.IsNullOrWhiteSpace(cell?.ToString())))
                        continue;
            
                    // Validate required columns with descriptive error
                    try
                    {
                        // Validate SemesterNumber (column 1)
                        if (!int.TryParse(row[1].ToString(), out _))
                            continue; // Skip invalid rows
            
                        var semesterNumber = Convert.ToInt32(row[1]);
                        
                        // Validate Semester (column 2)
                        var semester = row[2].ToString()?.Trim();
            
                        // Validate SubjectCode (column 3)
                        var subjectCode = row[3].ToString()?.Trim();
                        if (string.IsNullOrEmpty(subjectCode))
                        {
                            response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Cột 'Mã môn học' (cột 4) không được để trống");
                            return false;
                        }
            
                        // Validate SubjectName (column 6)
                        var subjectName = row[6].ToString()?.Trim();
                        if (string.IsNullOrEmpty(subjectName))
                        {
                            response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Cột 'Tên môn học' (cột 7) không được để trống");
                            return false;
                        }
                        
                        // Validate Status (column 9)
                        var status = row[9].ToString()?.Trim();
                        if (string.IsNullOrEmpty(status))
                        {
                            response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Cột 'Trạng thái' (cột 10) không được để trống");
                            return false;
                        }
                        
                        // Check row[10] for asterisk (*) - skip grade validation and don't save to database
                        var column10Value = row.ItemArray.Length > 10 ? row[10].ToString()?.Trim() : null;
                        if (!string.IsNullOrEmpty(column10Value) && column10Value.Contains("*"))
                        {
                            continue;
                        }

                        // Validate Credit (column 7)
                        var creditStr = row[7].ToString()?.Trim();
                        var credit = 0;
                        if (!string.IsNullOrEmpty(creditStr) && (status != ConstantEnum.StudentTranscriptStatus.Studying.GetDescription() && 
                                                                status != ConstantEnum.StudentTranscriptStatus.NotStarted.GetDescription()))
                        {
                            if (!int.TryParse(creditStr, out var creditOut))
                            {
                                response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Cột 'Số tín chỉ' (cột 8) phải là số nguyên");
                                return false;
                            }
                            credit = creditOut;
                        }

                        // Validate Grade (column 8)
                        var gradeStr = row[8].ToString()?.Trim();
                        if (string.IsNullOrEmpty(gradeStr) && (status == ConstantEnum.StudentTranscriptStatus.Studying.GetDescription() || status == ConstantEnum.StudentTranscriptStatus.NotStarted.GetDescription()))
                        {
                            if (status == ConstantEnum.StudentTranscriptStatus.Studying.GetDescription())
                            {
                                validSemesterNumbers.Add((semesterNumber, status));
                            }
                            continue;
                        }
                        double parsedGrade = 0;
                        if (!string.IsNullOrEmpty(gradeStr))
                        {
                            if (double.TryParse(gradeStr, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var g))
                            {
                                parsedGrade = g;
                            }
                            else if (int.TryParse(gradeStr, out var gi))
                            {
                                parsedGrade = gi;
                            }
                            else
                            {
                                response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Cột 'Điểm' (cột 9) phải là số thực");
                                return false;}
                        }

                        var statusNormalized = status?.Trim() ?? string.Empty;

                        if (string.IsNullOrWhiteSpace(semester) &&
                            !(string.Equals(statusNormalized, ConstantEnum.StudentTranscriptStatus.Studying.GetDescription(), StringComparison.OrdinalIgnoreCase)
                              || string.Equals(statusNormalized, ConstantEnum.StudentTranscriptStatus.NotStarted.GetDescription(), StringComparison.OrdinalIgnoreCase)))
                        {
                            response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Cột 'Học kỳ' (cột 3) không được để trống");
                            return false;
                        }
                        
                        var subject = new StudentTranscript
                        {
                            SemesterNumber = semesterNumber,
                            Semester = semester,
                            SubjectCode = subjectCode,
                            Prerequisite = row[4].ToString()?.Trim(),
                            SubjectName = subjectName,
                            Credit = credit,
                            Grade = parsedGrade,
                            Status = status,
                            StudentId = currentUser.UserId
                        };
                        
                        semesterIdSelectsEvent.SemesterNumbers.Add(subject.SemesterNumber);
                        studentTranscripts.Add(subject);
                    }
                    catch (IndexOutOfRangeException)
                    {
                        response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: File không đúng định dạng. Thiếu cột bắt buộc");
                        return false;
                    }
                    catch (Exception)
                    {
                        response.SetMessage(MessageId.E00000, $"Dòng {i + 1}: Lỗi xử lý dữ liệu");
                        return false;
                    }
                }
            
                // Validate that we have at least one valid transcript
                if (!studentTranscripts.Any())
                {
                    response.SetMessage(MessageId.E00000, "File không có dữ liệu bảng điểm hợp lệ");
                    return false;
                }
                
                // Check if max semester of "Studying" subjects matches student's current semester
                if (validSemesterNumbers.Any())
                {
                    var maxStudyingSemester = validSemesterNumbers.Max(x => x.key);
                    
                    // If student doesn't have semester info, auto-update based on transcript
                    if (!studentCurrentSemesterNumber.HasValue)
                    {
                        // Get semester ID from maxStudyingSemester
                        var tempSemesterResponse = await _requestClientSemesterIdSelects.GetResponse<SemesterIdSelectsEventResponse>(
                            new SemesterIdSelectsEvent
                            {
                                SemesterNumbers = new List<int> { maxStudyingSemester }
                            }, cancellationToken);
                        
                        if (tempSemesterResponse.Message.Success && tempSemesterResponse.Message.Response.Any())
                        {
                            var semesterId = tempSemesterResponse.Message.Response[0].SemesterId;
                            var semesterName = tempSemesterResponse.Message.Response[0].SemesterName;
                            
                            // Update student's semester - query from write repository
                            var studentToUpdate = await _studentRepository.FirstOrDefaultAsync(
                                x => x.StudentId == currentUser.UserId && x.IsActive,
                                cancellationToken: cancellationToken);
                            
                            studentToUpdate!.SemesterId = semesterId;
                            _studentRepository.Update(studentToUpdate);
                            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
                            
                            studentSelect!.SemesterId = semesterId;
                            studentSelect.SemesterName = semesterName;

                            _unitOfWork.Store(studentSelect);
                            await _unitOfWork.SessionSaveChangesAsync();
                        }
                    }
                    // If student has semester info, validate it matches
                    else if (maxStudyingSemester != studentCurrentSemesterNumber.Value)
                    {
                        response.SetMessage(MessageId.E00000, $"Kỳ học trong hồ sơ của bạn (Kỳ {studentCurrentSemesterNumber.Value}) không giống với bảng điểm bạn đang học (Kỳ {maxStudyingSemester}), vui lòng cập nhật lại một trong hai");
                        return false;
                    }
                }
            
                semesterIdSelectsEvent.SemesterNumbers = semesterIdSelectsEvent.SemesterNumbers.Distinct().ToList();
            
                // Publish event to CourseService to get semester IDs
                var semesterIdResponse = await _requestClientSemesterIdSelects.GetResponse<SemesterIdSelectsEventResponse>(semesterIdSelectsEvent, cancellationToken);
                if (!semesterIdResponse.Message.Success)
                {
                    response.SetMessage(MessageId.E00000, "Có lỗi xảy ra trong quá trình xử lý");
                    return false;
                }
            
                // Map semester IDs to transcripts
                var semesterIdMap = semesterIdResponse.Message.Response.ToDictionary(x => x.SemesterNumber, x => x.SemesterId);
                foreach (var transcript in studentTranscripts)
                {
                    if (semesterIdMap.TryGetValue(transcript.SemesterNumber, out var semesterId))
                    {
                        transcript.SemesterId = semesterId;
                    }
                }
            
                await _studentTranscriptRepository.AddRangeAsync(studentTranscripts);
                await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            }
            catch (Exception)
            {
                response.SetMessage(MessageId.E00000, $"Định dạng file không hợp lệ");
                return false;
            }
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Import bảng điểm");
            return true;
        }, cancellationToken);
        return response;
    }

    public async Task<StudentTranscriptSelectResponse> SelectStudentTranscriptAsync(StudentTranscriptSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new StudentTranscriptSelectResponse { Success = false };
        
        // Get student transcripts from cache or database
        var studentTranscripts = await _studentTranscriptRepository
            .Find(x => x.StudentId == _identityService.GetCurrentUser()!.UserId && x.IsActive)
            .OrderBy(x => x.SemesterNumber)
            .ThenBy(x => x.SubjectCode)
            .ToListAsync(cancellationToken: cancellationToken);
        if (!studentTranscripts.Any())
        {
            response.SetMessage(MessageId.I00000, "Chưa có bảng điểm nào được nhập");
            return response;
        }
        var transcriptItems = studentTranscripts.Select(st => new StudentTranscriptSelectResponseEntity
        {
            StudentTranscriptId = st.StudentTranscriptId,
            Semester = st.Semester,
            SemesterNumber = st.SemesterNumber,
            SubjectCode = st.SubjectCode,
            Prerequisite = st.Prerequisite,
            SubjectName = st.SubjectName,
            Credit = st.Credit,
            Grade = st.Grade,
            Status = st.Status,
            CreatedAt = st.CreatedAt
        }).ToList();
        
        // True
        response.Success = true;
        response.Response = transcriptItems;
        response.SetMessage(MessageId.I00001, "Lấy bảng điểm");
        return response;
    }

    /// <summary>
    /// Select student technologies and learning goals
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentTechnologyGoalSelectResponse> SelectStudentTechnologyGoalAsync(StudentTechnologyGoalSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new StudentTechnologyGoalSelectResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser();
        if (currentUser == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin người dùng");
            return response;
        }

        // Get student collection with technologies and learning goals
        var studentCollection = await _studentQueryRepository.FirstOrDefaultAsync(x =>
            x.StudentId == currentUser.UserId && x.IsActive);
        if (studentCollection == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin sinh viên");
            return response;
        }
        
        // Map technologies
        var technologies = studentCollection.Technologies?.Select(t => new StudentTechnologyItem
        {
            TechnologyId = t.TechnologyId,
            TechnologyName = t.Technology.TechnologyName,
            TechnologyType = t.Technology.TechnologyType,
            TechnologyTypeName = GetTechnologyTypeName(t.Technology.TechnologyType)
        }).ToList() ?? new List<StudentTechnologyItem>();
        
        // Map learning goals
        var learningGoals = studentCollection.LearningGoals?.Select(lg => new StudentLearningGoalItem
        {
            GoalId = lg.GoalId,
            GoalName = lg.Goal!.GoalName,
            LearningGoalType = lg.Goal.LearningGoalType,
            LearningGoalTypeName = GetLearningGoalTypeName(lg.Goal.LearningGoalType)
        }).ToList() ?? new List<StudentLearningGoalItem>();

        response.Success = true;
        response.Response = new StudentTechnologyGoalSelectResponseEntity
        {
            Semester = new SemesterItem
            {
                SemesterId = studentCollection.SemesterId,
                SemesterName = studentCollection.SemesterName
            },
            Major = new MajorItem
            {
                MajorId = studentCollection.MajorId,
                MajorName = studentCollection.MajorName
            },
            Technologies = technologies,
            LearningGoals = learningGoals
        };
        response.SetMessage(MessageId.I00001, "Lấy thông tin công nghệ và mục tiêu học tập");
        return response;
    }

    private static string GetTechnologyTypeName(short technologyType)
    {
        return technologyType switch
        {
            (short)ConstantEnum.TechnologyType.ProgrammingLanguage => "Ngôn ngữ lập trình",
            (short)ConstantEnum.TechnologyType.Framework => "Framework",
            _ => "Khác"
        };
    }
    
    private static string GetLearningGoalTypeName(short learningGoalType)
    {
        return learningGoalType switch
        {
            (short)ConstantEnum.LearningGoalType.None => "Chưa xác định",
            (short)ConstantEnum.LearningGoalType.Frontend => "Frontend",
            (short)ConstantEnum.LearningGoalType.Backend => "Backend",
            (short)ConstantEnum.LearningGoalType.Fullstack => "Fullstack",
            (short)ConstantEnum.LearningGoalType.Mobile => "Mobile",
            (short)ConstantEnum.LearningGoalType.Devops => "DevOps",
            (short)ConstantEnum.LearningGoalType.DataScience => "Data Science",
            (short)ConstantEnum.LearningGoalType.AI => "AI",
            (short)ConstantEnum.LearningGoalType.CloudComputing => "Cloud Computing",
            (short)ConstantEnum.LearningGoalType.CyberSecurity => "Cyber Security",
            _ => "Khác"
        };
    }
}