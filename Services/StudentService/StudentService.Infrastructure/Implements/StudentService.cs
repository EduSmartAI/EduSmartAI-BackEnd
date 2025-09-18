using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.InsertUserEvents;
using BuildingBlocks.Messaging.Events.StudentInformationInsertEvents;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.Students.Commands.Inserts;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class StudentService : IStudentService
{
    private readonly ICommandRepository<Student> _studentRepository;
    private readonly ICommandRepository<StudentTechnology> _studentTechnologyRepository;
    private readonly ICommandRepository<StudentLearningGoal> _studentLearningGoalRepository;
    private readonly IQueryRepository<StudentCollection> _studentQueryRepository;
    private readonly IQueryRepository<StudentLearningGoalCollection> _studentLearningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentQueryRepository"></param>
    /// <param name="studentRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="studentTechnologyRepository"></param>
    /// <param name="studentLearningGoalRepository"></param>
    /// <param name="studentLearningGoalQueryRepository"></param>
    public StudentService(IQueryRepository<StudentCollection> studentQueryRepository,
        ICommandRepository<Student> studentRepository, IUnitOfWork unitOfWork,
        ICommandRepository<StudentTechnology> studentTechnologyRepository,
        ICommandRepository<StudentLearningGoal> studentLearningGoalRepository,
        IQueryRepository<StudentLearningGoalCollection> studentLearningGoalQueryRepository)
    {
        _studentQueryRepository = studentQueryRepository;
        _studentRepository = studentRepository;
        _unitOfWork = unitOfWork;
        _studentTechnologyRepository = studentTechnologyRepository;
        _studentLearningGoalRepository = studentLearningGoalRepository;
        _studentLearningGoalQueryRepository = studentLearningGoalQueryRepository;
    }

    /// <summary>
    /// Insert new student
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UserInsertEventResponse> InsertStudentAsync(StudentInsertCommand request,
        CancellationToken cancellationToken = default)
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

            // Insert into StudentCollection
            var studentCollection = new StudentCollection
            {
                StudentId = request.UserId,
                FirstName = request.FirstName,
                LastName = request.LastName
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
    /// Insert student information about major, semester
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentInformationMajorSemesterEventResponse> InsertStudentMajorSemesterInformationAsync(StudentMajorSemesterInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentInformationMajorSemesterEventResponse { Success = false };

        // Check student exist
        var studentExist = await _studentRepository.FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IsActive, cancellationToken);
        if (studentExist == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin sinh viên");
            return response;
        }

        // Check student collection exist
        var studentCollection = await _studentQueryRepository.FirstOrDefaultAsync(x => x.StudentId == request.StudentId && x.IsActive);
        if (studentCollection == null)
        {
            response.SetMessage(MessageId.E99002);
            return response;
        }
        
        // Check if all TechnologyIds exist
        var existingTechIds = await _studentTechnologyRepository
            .Find(t  => request.TechnologyIds.Contains(t.TechnologyId))
            .Select(t => t.TechnologyId)
            .ToListAsync(cancellationToken);
        
        var missingTechIds = request.TechnologyIds.Except(existingTechIds).ToList();
        if (missingTechIds.Any())
        {
            response.SetMessage(MessageId.E00000, $"Không tìm thấy Công nghệ phù hợp");
            return response;
        }
        
        // Check if all LearningGoalIds exist
        var existingGoalIds = await _studentLearningGoalRepository
            .Find(g => request.LearningGoalIds.Contains(g.GoalId))
            .Select(g => g.GoalId)
            .ToListAsync(cancellationToken);
        var missingGoalIds = request.LearningGoalIds.Except(existingGoalIds).ToList();
        if (missingGoalIds.Any())
        {
            response.SetMessage(MessageId.E00000, $"Không tìm thấy mục tiêu phù hợp");
            return response;
        }
        

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Update student information
            studentExist!.MajorId = request.MajorId;
            studentExist.SemesterId = request.SemesterId;

            _studentRepository.Update(studentExist);

            // Insert new technologies
            var newStudentTechnologies = request.TechnologyIds.Select(techId => new StudentTechnology
            {
                StudentId = request.StudentId,
                TechnologyId = techId
            }).ToList();

            await _studentTechnologyRepository.AddRangeAsync(newStudentTechnologies);
            

            // Insert new learning goals
            var newStudentLearningGoals = request.LearningGoalIds.Select(goalId => new StudentLearningGoal
            {
                StudentId = request.StudentId,
                GoalId = goalId
            }).ToList();

            await _studentLearningGoalRepository.AddRangeAsync(newStudentLearningGoals);

            await _unitOfWork.SaveChangesAsync(request.StudentId.ToString(), cancellationToken);
            
            foreach (var newStudentTechnology in newStudentTechnologies)
            {
                TechnologyCollection? technologyCollection = TechnologyCollection.FromWriteModel(newStudentTechnology.Technology);
                _unitOfWork.Store(StudentTechnologyCollection.FromWriteModel(newStudentTechnology, technologyCollection));
            }
            
            // Synchronize with read model
            foreach (var newStudentLearningGoal in newStudentLearningGoals)
            {
                LearningGoalCollection? learningGoalCollection = LearningGoalCollection.FromWriteModel(newStudentLearningGoal.Goal);
                _unitOfWork.Store(StudentLearningGoalCollection.FromWriteModel(newStudentLearningGoal, learningGoalCollection));
            }

            // Update student collection information
            studentCollection.MajorId = request.MajorId;
            studentCollection.SemesterId = request.SemesterId;
            studentCollection.SemesterName = request.SemesterName;
            studentCollection.MajorName = request.MajorName;

            _unitOfWork.Store(studentCollection);
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm thông tin sinh viên");
            return true;
        }, cancellationToken);
        return response;
    }
}