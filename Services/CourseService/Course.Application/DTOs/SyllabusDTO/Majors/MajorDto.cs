namespace Course.Application.DTOs.SyllabusDTO.Majors
{
	public sealed record MajorDto(
		Guid MajorId,
		string MajorCode,
		string MajorName,
		string Description,
		short CreditRequired
	);
}
