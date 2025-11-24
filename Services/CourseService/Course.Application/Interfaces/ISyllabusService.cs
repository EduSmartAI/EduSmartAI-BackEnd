using Course.Application.Subjects.Commands.AddSubjectToSyllabus;
using Course.Application.Syllabus.Commands.AddSemester;
using Course.Application.Syllabus.Commands.CreateSyllabus;
using Course.Application.Syllabus.Queries;

namespace Course.Application.Interfaces
{
	public interface ISyllabusService
	{
		Task<CreateSyllabusResponse> CreateSyllabusAsync(CreateSyllabusCommand cmd, CancellationToken ct);
		Task<AddSemesterResponse> AddSemesterAsync(AddSemesterCommand cmd, CancellationToken ct);
		Task<AddSubjectResponse> AddSubjectAsync(AddSubjectCommand cmd, CancellationToken ct);
		Task<GetFullSyllabusResponse> GetFullSyllabusAsync(string versionLabel, CancellationToken ct);
	}

}
