namespace Course.Application.DTOs.SyllabusDTO
{
	public record CloneFoundationSyllabusDto(
		string BaseVersion,   // K19
		string NewVersion,    // K20
		DateOnly EffectiveFrom,
		DateOnly? EffectiveTo
	);
}
