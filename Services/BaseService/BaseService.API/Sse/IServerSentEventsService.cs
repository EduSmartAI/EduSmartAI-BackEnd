using Microsoft.AspNetCore.Http;

namespace BaseService.API.Sse;

public interface IServerSentEventsService
{
    Task StreamAsync(HttpResponse response, Func<IServerSentEventsClient, CancellationToken, Task> streamAction, CancellationToken cancellationToken = default);
}




