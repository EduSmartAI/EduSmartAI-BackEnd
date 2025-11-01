using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyDeleteCommandHandler(ITechnologyService technologyService) : ICommandHandler<TechnologyDeleteCommand, TechnologyDeleteResponse>
{
    public async Task<TechnologyDeleteResponse> Handle(TechnologyDeleteCommand request, CancellationToken cancellationToken)
    {
        return await technologyService.DeleteTechnologyAsync(request, cancellationToken);
    }
}


