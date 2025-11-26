using BuildingBlocks.Messaging.Events.AuthService.InsertUserEvents;
using BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation;
using TeacherService.Application.Applications.Teachers.Commands.Inserts;
using TeacherService.Application.Applications.Teachers.Commands.UpdateTeacherProfile;
using TeacherService.Application.Applications.Teachers.Queries.GetTeacherBasicProfile;
using TeacherService.Application.Applications.Teachers.Queries.GetTeacherDetail;
using TeacherService.Application.DTOs;

namespace TeacherService.Application.Interfaces;

public interface ITeacherService
{
    Task<LecturerInsertEventResponse> InsertTeacherAsync(LecturerInsertCommand request, CancellationToken cancellationToken = default);
	Task<UpdateTeacherProfileResponse> UpdateTeacherProfileAsync(Guid teacherId, UpdateTeacherProfileRequest req, CancellationToken ct = default);
	Task<GetTeacherDetailResponse> GetTeacherDetailAsync(Guid teacherId,  CancellationToken ct = default);
	Task<GetTeacherBasicProfileResponse> GetBasicProfileAsync(Guid teacherId, CancellationToken ct = default);
	Task<List<TeacherNameExternalServiceDto>> GetTeacherNamesAsync(IList<Guid> teacherIds, CancellationToken ct = default);
}