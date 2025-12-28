namespace Course.Application.DTOs.SyllabusDTO
{
	public record CloneCascadeSyllabusDto(
		string BaseVersion,   // K19
		string NewVersion,    // K20
		string MajorCode,     // .NET
		DateOnly EffectiveFrom,
		DateOnly? EffectiveTo
	);
}
