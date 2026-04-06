using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class RegenerateLearningPathCommand : ICommand<RegenerateLearningPathCommandResponse>
{
    /// <summary>
    /// Optional: when triggered outside HTTP (e.g. from AiService chat), provide identity explicitly.
    /// If null, handler will use IIdentityService.GetCurrentUser().
    /// </summary>
    public Guid? StudentId { get; set; }

    /// <summary>
    /// Optional: email of the student triggering regeneration.
    /// </summary>
    public string? StudentEmail { get; set; }
}