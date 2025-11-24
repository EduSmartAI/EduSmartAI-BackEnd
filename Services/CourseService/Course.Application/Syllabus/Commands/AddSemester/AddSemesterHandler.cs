namespace Course.Application.Syllabus.Commands.AddSemester
{
	public class AddSemesterHandler(ISyllabusService service) : ICommandHandler<AddSemesterCommand, AddSemesterResponse>
	{
		public async Task<AddSemesterResponse> Handle(AddSemesterCommand request, CancellationToken cancellationToken)
			=> await service.AddSemesterAsync(request, cancellationToken);
	}
}
