using BaseService.Application.Interfaces.Repositories;
using Course.Application.Interfaces;
using Course.Domain.Models;
using MassTransit.Initializers;

namespace Course.Infrastructure.Implements;

public class MajorService(ICommandRepository<Major> commandRepository) : IMajorService
{
    /// <summary>
    /// Select MajorName by Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> SelectMajorAsync(Guid id, CancellationToken cancellationToken) 
        => await commandRepository.FirstOrDefaultAsync(x => x.MajorId == id, cancellationToken).Select(x => x.MajorName);
}