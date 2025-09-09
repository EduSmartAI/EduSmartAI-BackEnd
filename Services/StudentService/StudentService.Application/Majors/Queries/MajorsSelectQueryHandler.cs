using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Majors.Queries;

public class MajorsSelectQueryHandler : IQueryHandler<MajorsSelectQuery, MajorsSelectResponse>
{
    private readonly IMajorService _majorService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="majorService"></param>
    public MajorsSelectQueryHandler(IMajorService majorService)
    {
        _majorService = majorService;
    }

    public async Task<MajorsSelectResponse> Handle(MajorsSelectQuery request, CancellationToken cancellationToken)
    {
        return await _majorService.SelectMajorsAsync(request);
    }
}