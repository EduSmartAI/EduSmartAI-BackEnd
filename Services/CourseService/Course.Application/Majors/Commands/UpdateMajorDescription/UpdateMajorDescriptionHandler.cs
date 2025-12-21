namespace Course.Application.Majors.Commands.UpdateMajorDescription
{
	public class UpdateMajorDescriptionHandler(IMajorService _majorService) : ICommandHandler<UpdateMajorDescriptionCommand, UpdateMajorDescriptionResponse>
	{
		public async Task<UpdateMajorDescriptionResponse> Handle(UpdateMajorDescriptionCommand request, CancellationToken cancellationToken)
		{
			return await _majorService.UpdateMajorDescriptionAsync(request.MajorId, request.Description, cancellationToken);
		}
	}
}
