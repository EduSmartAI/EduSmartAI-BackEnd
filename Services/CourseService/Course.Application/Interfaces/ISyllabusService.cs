using Course.Application.DTOs.SyllabusDTO;
using Course.Application.Subjects.Commands.AddSubjectToSyllabus;
using Course.Application.Syllabus.Commands.CloneCascadeSyllabus;
using Course.Application.Syllabus.Commands.CloneFoundationSyllabus;
using Course.Application.Syllabus.Commands.CreateFullSyllabus;
using Course.Application.Syllabus.Commands.CreateSyllabus;
using Course.Application.Syllabus.Queries.GetFullSyllabus;

namespace Course.Application.Interfaces
{
	public interface ISyllabusService
	{
		Task<CreateSyllabusResponse> CreateSyllabusAsync(CreateSyllabusCommand cmd, CancellationToken ct);
		Task<CreateFullSyllabusResponse> CreateFullSyllabusAsync(CreateFullSyllabusDto dto, CancellationToken ct);
		Task<AddSubjectResponse> AddSubjectAsync(AddSubjectCommand cmd, CancellationToken ct);
		Task<GetFullSyllabusResponse> GetFullSyllabusAsync(string versionLabel, CancellationToken ct);
		Task<CloneCascadeSyllabusResponse> CloneCascadeAsync(CloneCascadeSyllabusDto dto, CancellationToken ct);
		Task<CloneFoundationSyllabusResponse> CloneFoundationAsync(CloneFoundationSyllabusDto dto, CancellationToken ct);

	}

}
