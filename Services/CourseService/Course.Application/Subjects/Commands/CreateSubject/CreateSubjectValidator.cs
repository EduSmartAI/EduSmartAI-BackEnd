namespace Course.Application.Subjects.Commands.CreateSubject
{
	public class CreateSubjectValidator : AbstractValidator<CreateSubjectCommand>
	{
		public CreateSubjectValidator()
		{
			RuleFor(x => x.CreateSubjectDto).NotNull().WithMessage("CreateSubjectDto cannot be null.");
			RuleFor(x => x.CreateSubjectDto.SubjectCode)
				.NotEmpty().WithMessage("Subject code is required.")
				.MaximumLength(10).WithMessage("Subject code must not exceed 10 characters.");
			RuleFor(x => x.CreateSubjectDto.SubjectName)
				.NotEmpty().WithMessage("Subject name is required.")
				.MaximumLength(100).WithMessage("Subject name must not exceed 100 characters.");
		}
	}
}
