using StudentService.Application.Applications.Technologies.Commands;

namespace StudentService.Application.Interfaces;

public interface ITechnologyService
{
    Task<TechnologyInsertResponse> InsertTechnologyAsync(TechnologyInsertCommand request, CancellationToken cancellationToken);
}