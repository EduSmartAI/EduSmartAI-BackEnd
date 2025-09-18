using Course.Domain.Models;

namespace Course.Application.Interfaces;

public interface ISemesterService
{
    Task<string> SelectSemesterAsync(Guid id, CancellationToken cancellationToken);
}