namespace Course.Application.Majors.Commands.CreateMajor
{
	public class CreateMajorValidator : AbstractValidator<CreateMajorCommand>
	{
		public CreateMajorValidator()
		{
			RuleFor(x => x.CreateMajorDto).NotNull().WithMessage("CreateMajorDto cannot be null.");
			RuleFor(x => x.CreateMajorDto.MajorCode)
				.NotEmpty().WithMessage("Major code is required.")
				.MaximumLength(10).WithMessage("Major code must not exceed 10 characters.");
			RuleFor(x => x.CreateMajorDto.MajorName)
				.NotEmpty().WithMessage("Major name is required.")
				.MaximumLength(100).WithMessage("Major name must not exceed 100 characters.");
			RuleFor(x => x.CreateMajorDto.Description)
				.MaximumLength(500).WithMessage("Description must not exceed 500 characters.");
		}
	}
}
