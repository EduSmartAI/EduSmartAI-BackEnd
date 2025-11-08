namespace Course.Application.DTOs.SyllabusDTO.Majors
{
	public sealed record CreateMajorDto(
		string MajorCode,
		string MajorName,
		string? Description
	);
}
