using BuildingBlocks.CQRS;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.Technologies.Queries;

public class AdminTechnologiesSelectQueryHandler(ITechnologyService technologyService) : IQueryHandler<AdminTechnologiesSelectQuery, AdminTechnologiesSelectResponse>
{
    public async Task<AdminTechnologiesSelectResponse> Handle(AdminTechnologiesSelectQuery request, CancellationToken cancellationToken)
    {
        return await technologyService.SelectAdminTechnologiesAsync(request);
    }
}


