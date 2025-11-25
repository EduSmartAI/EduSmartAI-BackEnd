using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using Course.Application.Subjects.Commands.CreateSubject;
using Course.Application.Subjects.Queries.GetSubjectDetails;
using Course.Application.Subjects.Queries.GetSubjects;
using Course.Application.Subjects.Queries.SelectSubject;

namespace Course.Application.Interfaces;

public interface ISubjectService
{
    Task<SubjectSelectsEventResponse> SelectSubject(SubjectSelectsQuery request, CancellationToken cancellationToken);
    Task<CreateSubjectResponse> CreateSubjectAsync(CreateSubjectCommand request, CancellationToken cancellationToken);
	Task<GetSubjectsResponse> GetSubjectsAsync(int? page, int? size, string? search, CancellationToken ct = default);
	Task<GetSubjectDetailResponse> GetSubjectDetailAsync(Guid subjectId, CancellationToken ct = default);
}