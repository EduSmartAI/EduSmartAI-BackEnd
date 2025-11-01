using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyUpdateCommandHandler(ITechnologyService technologyService) : ICommandHandler<TechnologyUpdateCommand, TechnologyUpdateResponse>
{
    public async Task<TechnologyUpdateResponse> Handle(TechnologyUpdateCommand request, CancellationToken cancellationToken)
    {
        return await technologyService.UpdateTechnologyAsync(request, cancellationToken);
    }
}