namespace Course.Application.Majors.Commands.UpdateMajorDescription
{
	public record UpdateMajorDescriptionCommand(Guid MajorId, string? Description) : ICommand<UpdateMajorDescriptionResponse>;

	public record UpdateMajorDescriptionResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get ; set ; }
	}
}
