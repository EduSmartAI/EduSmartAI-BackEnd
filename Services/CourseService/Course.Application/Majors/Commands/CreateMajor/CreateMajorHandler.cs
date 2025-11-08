namespace Course.Application.Majors.Commands.CreateMajor
{
	public class CreateMajorHandler(IMajorService _majorService) : ICommandHandler<CreateMajorCommand, CreateMajorResponse>
	{
		public async Task<CreateMajorResponse> Handle(CreateMajorCommand request, CancellationToken cancellationToken)
		{
			return await _majorService.CreateMajorAsync(request, cancellationToken);
		}
	}
}
