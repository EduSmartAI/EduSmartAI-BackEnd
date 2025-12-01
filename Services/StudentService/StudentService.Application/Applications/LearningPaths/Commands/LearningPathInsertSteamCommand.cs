using BuildingBlocks.CQRS;
using System.ComponentModel.DataAnnotations;

namespace StudentService.Application.Applications.LearningPaths.Commands
{
    public class LearningPathInsertSteamCommand : ICommand<LearningPathInsertResponse>
    {
        public Guid PathId { get; set; }
        [Required]
        [MaxLength(255)]
        public string PathName { get; set; } = string.Empty;
    }
}
