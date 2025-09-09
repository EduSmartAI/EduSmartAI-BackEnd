using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Majors.Commands;

public class MajorInsertCommandHandler : ICommandHandler<MajorInsertCommand, MajorInsertResponse>
{
    private readonly IMajorService _majorService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="majorService"></param>
    public MajorInsertCommandHandler(IMajorService majorService)
    {
        _majorService = majorService;
    }

    /// <summary>
    /// Handle insert major command
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<MajorInsertResponse> Handle(MajorInsertCommand request, CancellationToken cancellationToken)
    {
        return await _majorService.InsertMajorAsync(request, cancellationToken);
    }
}