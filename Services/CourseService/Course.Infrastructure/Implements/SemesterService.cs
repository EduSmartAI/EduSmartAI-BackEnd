using BaseService.Application.Interfaces.Repositories;
using Course.Application.Interfaces;
using Course.Domain.Models;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class SemesterService(ICommandRepository<Semester> commandRepository) : ISemesterService
{
    /// <summary>
    /// Select SemesterName by Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SelectSemesterAsync(Guid id, CancellationToken cancellationToken)
        => await commandRepository.FirstOrDefaultAsync(x => x.SemesterId == id, cancellationToken).Select(x => x.SemesterName);
}