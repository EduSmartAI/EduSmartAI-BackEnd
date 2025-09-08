namespace BaseService.API.BaseControllers;

public interface IApiAsyncController<TRequest, TResponse>
{
    Task<TResponse> ProcessRequest(TRequest request);
}