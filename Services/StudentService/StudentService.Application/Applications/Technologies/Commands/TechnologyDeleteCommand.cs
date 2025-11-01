using System.ComponentModel.DataAnnotations;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyDeleteCommand : ICommand<TechnologyDeleteResponse>
{
    [Required(ErrorMessage = "TechnologyId is required")]
    public Guid TechnologyId { get; set; }
}

