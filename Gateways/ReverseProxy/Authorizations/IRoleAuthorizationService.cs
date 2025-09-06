namespace ReverseProxy.Authorizations;

public interface IRoleAuthorizationService
{
    /// <summary>
    /// Check if the user has access to the specified service, path, and method
    /// </summary>
    /// <param name="servicePrefix"></param>
    /// <param name="relativePath"></param>
    /// <param name="method"></param>
    /// <param name="user"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> CheckAccessAsync(string servicePrefix, string relativePath, string method, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken = default);
}