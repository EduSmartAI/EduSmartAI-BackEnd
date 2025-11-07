namespace Course.Application.Courses.Commands.CreateLessonTranscript
{
	public record CreateLessonTranscriptCommand(
		Guid LessonId,
		string Language,
		short Status,
		string? TextFull,
		string? VttUrl,
		string? VttPublicId,
		string? Error
	) : ICommand<CreateLessonTranscriptResponse>;

	public record CreateLessonTranscriptResponse : AbstractApiResponse<string>
	{
		public override string Response { get; set; } = default!;
	}
}
