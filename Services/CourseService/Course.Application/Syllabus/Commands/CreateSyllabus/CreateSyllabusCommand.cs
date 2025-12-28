namespace Course.Application.Syllabus.Commands.CreateSyllabus
{
	public record CreateSyllabusCommand(
		Guid MajorId,
		string VersionLabel,
		DateOnly EffectiveFrom,
		DateOnly? EffectiveTo
	) : ICommand<CreateSyllabusResponse>;

	public record CreateSyllabusResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; }
	}

}
