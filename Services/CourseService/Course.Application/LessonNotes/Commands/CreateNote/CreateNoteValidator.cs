namespace Course.Application.LessonNotes.Commands.CreateNote
{
	public class CreateNoteValidator : AbstractValidator<CreateNoteCommand>
	{
		public CreateNoteValidator()
		{
			RuleFor(x => x.LessonId)
				.NotEmpty().WithMessage("LessonId is required.");
			RuleFor(x => x.TimeSeconds)
				.GreaterThanOrEqualTo(0).WithMessage("TimeSeconds must be greater than or equal to 0.");
			RuleFor(x => x.Content)
				.NotEmpty().WithMessage("Content is required.")
				.MaximumLength(5000).WithMessage("Content must not exceed 5000 characters.");
		}
	}
}
