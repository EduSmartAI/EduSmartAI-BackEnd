using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using Microsoft.EntityFrameworkCore;
using TeacherService.Application.Applications.Teachers.Commands.Inserts;
using TeacherService.Application.Applications.Teachers.Commands.UpdateTeacherProfile;
using TeacherService.Application.Applications.Teachers.Queries.GetTeacherDetail;
using TeacherService.Application.DTOs;
using TeacherService.Application.Interfaces;
using TeacherService.Domain.ReadModels;
using TeacherService.Domain.WriteModels;

namespace TeacherService.Infrastructure.Implements;

public class TeacherService(
	IUnitOfWork _unitOfWork,
    IIdentityService _identity,
	ICommandRepository<Teacher> _teacherCommandRepository,
    ICommandRepository<TeacherCertificate> _teacherCertificateCommandRepository,
    ICommandRepository<TeacherExperience> _teacherExperienceCommandRepository,
    ICommandRepository<TeacherQualification> _teacherQualificationCommandRepository,
	IQueryRepository<TeacherCollection> _teacherQueryRepository
)
: ITeacherService
{
	public async Task<GetTeacherDetailResponse> GetDetailAsync(Guid teacherId, CancellationToken ct = default)
	{
		var response = new GetTeacherDetailResponse { Success = false };

		// Load teacher + child tables
		var teacher = await _teacherCommandRepository.FirstOrDefaultAsync(
			x => x.TeacherId == teacherId && x.IsActive,
			ct
		);

		if (teacher is null)
		{
			response.SetMessage(MessageId.E00000, "Không tìm thấy giảng viên");
			return response;
		}

		// Load certificates
		var certificates = await _teacherCertificateCommandRepository
			.Find(c => c.TeacherId == teacherId && c.IsActive, false, ct)
			.Select(c => new TeacherCertificateDto(
				c.CertificateId,
				c.CertName,
				c.Issuer,
				c.IssuedDate,
				c.ExpireDate,
				c.CertUrl
			)).ToListAsync(ct);

		// Load experiences
		var experiences = await _teacherExperienceCommandRepository
			.Find(c => c.TeacherId == teacherId && c.IsActive, false, ct)
			.Select(c => new TeacherExperienceDto(
				c.ExperienceId,
				c.RoleTitle,
				c.Organization,
				c.StartDate,
				c.EndDate,
				c.IsCurrent,
				c.Description
			)).ToListAsync(ct);

		// Load qualifications
		var qualifications = await _teacherQualificationCommandRepository
			.Find(c => c.TeacherId == teacherId && c.IsActive, false, ct)
			.Select(c => new TeacherQualificationDto(
				c.QualificationId,
				c.DegreeTitle,
				c.Institution,
				c.StartDate,
				c.EndDate,
				c.Description,
				c.CertificateUrl
			)).ToListAsync(ct);

		response.Success = true;
		response.SetMessage(MessageId.I00001);

		response.Response = new TeacherDetailDto(
			teacher.TeacherId,
			teacher.DisplayName,
			teacher.FirstName,
			teacher.LastName,
			teacher.Bio,
			teacher.ProfilePictureUrl,
			certificates,
			experiences,
			qualifications
		);

		return response;
	}

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

	/// <summary>
	/// Update teacher profile
	/// </summary>
	/// <param name="teacherId"></param>
	/// <param name="req"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	public async Task<UpdateTeacherProfileResponse> UpdateTeacherProfileAsync(Guid teacherId, UpdateTeacherProfileRequest req, CancellationToken ct = default)
	{
		var response = new UpdateTeacherProfileResponse { Success = false };

		var current = _identity.GetCurrentUser();
		if (current is null)
		{
			response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
			return response;
		}

		var teacher = await _teacherCommandRepository
			.FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.IsActive, ct);

		if (teacher is null)
		{
			response.SetMessage(MessageId.E00000, "Không tìm thấy giảng viên");
			return response;
		}

		var newDisplayName = string.IsNullOrWhiteSpace(req.DisplayName)
			? string.Concat(req.FirstName, " ", req.LastName).Trim()
			: req.DisplayName;

		teacher.DisplayName = newDisplayName;
		teacher.FirstName = req.FirstName;
		teacher.LastName = req.LastName;
		teacher.Bio = req.Bio;
		teacher.ProfilePictureUrl = req.ProfilePictureUrl;

		await _unitOfWork.BeginTransactionAsync(async () =>
		{
			_teacherCommandRepository.Update(teacher, current.Email);
			await _unitOfWork.SaveChangesAsync(current.Email, ct);

			// Cập nhật trong TeacherCollection
			var teacherCollection = await _teacherQueryRepository
			.FirstOrDefaultAsync(x => x.TeacherId == teacherId && x.IsActive);

			if (teacherCollection is not null)
			{
				teacherCollection.DisplayName = newDisplayName;
				teacherCollection.FirstName = req.FirstName;
				teacherCollection.LastName = req.LastName;
				teacherCollection.Bio = req.Bio;
				teacherCollection.ProfilePictureUrl = req.ProfilePictureUrl;
				teacherCollection.UpdatedAt = DateTime.UtcNow;
				teacherCollection.UpdatedBy = current.Email;

				_unitOfWork.Store(teacherCollection);
				await _unitOfWork.SessionSaveChangesAsync();
			}

			return true;
		}, ct);

		response.Success = true;
		response.SetMessage(MessageId.I00001, "Cập nhật hồ sơ giảng viên");
		response.Response = true;

		return response;
	}
}