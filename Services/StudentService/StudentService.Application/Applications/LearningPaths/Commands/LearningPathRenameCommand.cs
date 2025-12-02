using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class LearningPathRenameCommand : ICommand<LearningPathRenameResponse>
{
    [Required]
    public Guid LearningPathId { get; set; }

    [Required]
    [MaxLength(255)]
    public string PathName { get; set; } = string.Empty;

    public Guid StudentId { get; set; }

    public string StudentEmail { get; set; } = string.Empty;
}




