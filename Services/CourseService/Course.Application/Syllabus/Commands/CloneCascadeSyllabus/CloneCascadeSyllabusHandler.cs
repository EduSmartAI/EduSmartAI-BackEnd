namespace Course.Application.Syllabus.Commands.CloneCascadeSyllabus
{
	public class CloneCascadeSyllabusHandler(ISyllabusService service) : ICommandHandler<CloneCascadeSyllabusCommand, CloneCascadeSyllabusResponse>
	{
		public async Task<CloneCascadeSyllabusResponse> Handle(CloneCascadeSyllabusCommand request, CancellationToken ct)
			=> await service.CloneCascadeAsync(request.CloneCascadeSyllabusDto, ct);
	}
}
