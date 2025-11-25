namespace Course.Application.Syllabus.Commands.CloneFoundationSyllabus
{
	public class CloneFoundationSyllabusHandler(ISyllabusService service) : ICommandHandler<CloneFoundationSyllabusCommand, CloneFoundationSyllabusResponse>
	{
		public async Task<CloneFoundationSyllabusResponse> Handle(CloneFoundationSyllabusCommand request, CancellationToken ct)
			=> await service.CloneFoundationAsync(request.CloneFoundationSyllabusDto, ct);
	}
}
