using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using TeacherService.Application.Applications.Teachers.Commands.Inserts;
using TeacherService.Application.Interfaces;
using TeacherService.Domain.ReadModels;
using TeacherService.Domain.WriteModels;

namespace TeacherService.Infrastructure.Implements;

public class TeacherService(
	IUnitOfWork _unitOfWork,
	ICommandRepository<Teacher> _teacherCommandRepository,
    ICommandRepository<TeacherCertificate> _teacherCertificateCommandRepository,
    ICommandRepository<TeacherExperience> _teacherExperienceCommandRepository,
    ICommandRepository<TeacherQualification> _teacherQualificationCommandRepository,
	IQueryRepository<TeacherCollection> _teacherQueryRepository
)
: ITeacherService
{
    /// <summary>
    /// Insert new teacher
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LecturerInsertEventResponse> InsertTeacherAsync(LecturerInsertCommand request, CancellationToken cancellationToken = default)
    {
        var response = new LecturerInsertEventResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new Teacher
            var displayName = string.Concat(request.FirstName, " ", request.LastName);
            var teacher = new Teacher
            {
                TeacherId = request.UserId,
                DisplayName = displayName,
                FirstName = request.FirstName,
                LastName = request.LastName
            };

            await _teacherCommandRepository.AddAsync(teacher, request.Email);

            // If OldUserId is not null, delete the old teacher record
            if (request.OldUserId.HasValue)
            {
                // Check if the old teacher exists
                var oldTeacher = await _teacherCommandRepository.FirstOrDefaultAsync(x => x.TeacherId == request.OldUserId && x.IsActive, cancellationToken);
                if (oldTeacher != null)
                {
                    _teacherCommandRepository.Update(oldTeacher, request.Email, true);

                    // Delete the associated TeacherCollection if it exists
                    var oldTeacherCollection = await _teacherQueryRepository.FirstOrDefaultAsync(x => x.TeacherId == request.OldUserId && x.IsActive);
                    if (oldTeacherCollection != null)
                    {
                        _unitOfWork.Delete(oldTeacherCollection);
                        await _unitOfWork.SessionSaveChangesAsync();
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var now = DateTime.UtcNow;
            // Insert into TeacherCollection
            var teacherCollection = new TeacherCollection
            {
                TeacherId = request.UserId,
                DisplayName = displayName,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = now,
                CreatedBy = request.Email,
                UpdatedAt = now,
                UpdatedBy = request.Email,
                IsActive = true
            };
            _unitOfWork.Store(teacherCollection);
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Đăng ký");
            return true;
        }, cancellationToken);
        
        return response;
    }
}