namespace Course.Application.Syllabus.Commands.CreateFullSyllabus
{
	public class CreateFullSyllabusHandler(ISyllabusService service) : ICommandHandler<CreateFullSyllabusCommand, CreateFullSyllabusResponse>
	{
		public async Task<CreateFullSyllabusResponse> Handle(CreateFullSyllabusCommand request, CancellationToken ct)
			=> await service.CreateFullSyllabusAsync(request.CreateFullSyllabusDto, ct);
	}

}
