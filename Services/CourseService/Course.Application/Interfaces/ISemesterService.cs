using BuildingBlocks.Messaging.Events.QuizService.SemesterSelectsEvents;
using Course.Application.Semesters.Queries.GetSemesterDetails;
using Course.Application.Semesters.Queries.GetSemesters;
using Course.Application.Semesters.Queries.SelectSemesters;
using Course.Domain.Models;

namespace Course.Application.Interfaces;

public interface ISemesterService
{
    Task<Semester?> SelectSemesterAsync(Guid id, CancellationToken cancellationToken);
    Task<SemesterSelectsEventResponse> SelectSemestersAsync(SemesterSelectsQuery request, CancellationToken cancellationToken);
	Task<GetSemestersResponse> GetSemestersAsync(int? page, int? size, string? search, CancellationToken ct = default);
	Task<GetSemesterDetailResponse> GetSemesterDetailAsync(Guid semesterId, CancellationToken ct = default);
}