
namespace Course.Application.Subjects.Commands.CreateSubject
{
	public class CreateSubjectHandler(ISubjectService _subjectService) : ICommandHandler<CreateSubjectCommand, CreateSubjectResponse>
	{
		public async Task<CreateSubjectResponse> Handle(CreateSubjectCommand request, CancellationToken cancellationToken)
		{
			return await _subjectService.CreateSubjectAsync(request, cancellationToken);
		}
	}
}
