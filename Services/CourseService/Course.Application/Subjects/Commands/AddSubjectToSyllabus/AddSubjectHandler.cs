namespace Course.Application.Subjects.Commands.AddSubjectToSyllabus
{
	public class AddSubjectHandler(ISyllabusService service) : ICommandHandler<AddSubjectCommand, AddSubjectResponse>
	{
		public async Task<AddSubjectResponse> Handle(AddSubjectCommand request, CancellationToken cancellationToken)
			=> await service.AddSubjectAsync(request, cancellationToken);
	}
}
