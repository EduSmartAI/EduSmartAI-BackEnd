using Course.Application.DTOs;
using FluentValidation;

namespace Course.Application.Courses.Commands.CreateCourse
{
	public class CreateCourseValidator : AbstractValidator<CreateCourseCommand>
	{
		public CreateCourseValidator()
		{
			RuleFor(x => x.Payload.TeacherId).NotEmpty();
			RuleFor(x => x.Payload.SubjectId).NotEmpty();
			RuleFor(x => x.Payload.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.Payload.Price).GreaterThan(0);
			RuleFor(x => x.Payload.DealPrice)
				.Must((cmd, deal) => deal is null || deal <= cmd.Payload.Price)
				.WithMessage("DealPrice must be <= Price.");
			RuleForEach(x => x.Payload.Modules).SetValidator(new CreateModuleValidator());

			// Không trùng PositionIndex trong Modules
			RuleFor(x => x.Payload.Modules.Select(m => m.PositionIndex))
				.Must(list => list.Distinct().Count() == list.Count())
				.WithMessage("Module PositionIndex must be unique within the course.");
		}
	}

	public class CreateModuleValidator : AbstractValidator<CreateModuleDto>
	{
		public CreateModuleValidator()
		{
			RuleFor(x => x.ModuleName).NotEmpty().MaximumLength(200);
			RuleForEach(x => x.Lessons).SetValidator(new CreateLessonValidator());
			RuleFor(x => x.Lessons.Select(l => l.PositionIndex))
				.Must(list => list.Distinct().Count() == list.Count())
				.WithMessage("Lesson PositionIndex must be unique within the module.");
		}
	}

	public class CreateLessonValidator : AbstractValidator<CreateLessonDto>
	{
		public CreateLessonValidator()
		{
			RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
			RuleFor(x => x.VideoUrl).NotEmpty().MaximumLength(1000);
		}
	}
}
