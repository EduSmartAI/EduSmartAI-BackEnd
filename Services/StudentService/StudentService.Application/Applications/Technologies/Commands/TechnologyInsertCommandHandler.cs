using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyInsertCommandHandler(ITechnologyService technologyService) : ICommandHandler<TechnologyInsertCommand, TechnologyInsertResponse>
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TechnologyInsertResponse> Handle(TechnologyInsertCommand request, CancellationToken cancellationToken)
    {
        return await technologyService.InsertTechnologyAsync(request, cancellationToken);
    }
}