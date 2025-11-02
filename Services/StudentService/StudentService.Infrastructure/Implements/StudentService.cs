using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
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
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxService;
    private readonly IQueryRepository<LearningGoalCollection> _learningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IRequestClient<MajorAndSemesterSelectEvent> _requestClientMajorAndSemesterSelect;
    private readonly IRequestClient<AvatarUploadEvent> _requestClientAvatarUpload;

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
        IRequestClient<AvatarUploadEvent> requestClientAvatarUpload)
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
           
            StudentLearningGoalCollection newStudentLearningGoalCollection = new StudentLearningGoalCollection();
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
            LearningGoalName = studentCollection.LearningGoals!.Select(x => x.Goal!.GoalName).FirstOrDefault()!,
            LearningGoalType = studentCollection.LearningGoals!.Select(x => x.Goal!.LearningGoalType).FirstOrDefault(),
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
                else
                {
                    studentExist.AvatarUrl = avatarUrlResponse.Message.Response.AvatarUrl;
                }
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
                        // If not exists, add new
                        studentExist.StudentTechnologies.Add(new StudentTechnology
                        {
                            StudentId = studentExist.StudentId,
                            TechnologyId = techId,
                        });
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
                        // If not exists, add new
                        studentExist.StudentLearningGoals.Add(new StudentLearningGoal
                        {
                            StudentId = studentExist.StudentId,
                            GoalId = goalId,
                        });
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