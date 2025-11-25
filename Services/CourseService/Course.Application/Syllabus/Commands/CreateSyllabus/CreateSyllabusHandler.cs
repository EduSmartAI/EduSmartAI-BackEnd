namespace Course.Application.Syllabus.Commands.CreateSyllabus
{
	public class CreateSyllabusHandler(ISyllabusService service) : ICommandHandler<CreateSyllabusCommand, CreateSyllabusResponse>
	{
		public async Task<CreateSyllabusResponse> Handle(CreateSyllabusCommand request, CancellationToken cancellationToken)
			=> await service.CreateSyllabusAsync(request, cancellationToken);
	}

}
