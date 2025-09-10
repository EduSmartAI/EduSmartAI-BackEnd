using NLog;

namespace ReverseProxy.Authorizations;

public class RoleAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IRoleAuthorizationService _roleAuthService;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next"></param>
    /// <param name="roleAuthService"></param>
    public RoleAuthorizationMiddleware(RequestDelegate next, IRoleAuthorizationService roleAuthService)
    {
        _next = next;
        _roleAuthService = roleAuthService;
    }
    
    /// <summary>
    /// Middleware processing
    /// </summary>
    /// <param name="context"></param>
    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";
        
        // Skip authorization for specific paths
        if (ShouldSkipAuthorization(context.Request.Path))
        {
            await _next(context);
            return;
        }
        
        
        // Extract service prefix and relative path
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            await _next(context);
            return;
        }

        // Service prefix is the first segment, relative path is the rest
        var servicePrefix = segments[0];
        var relative = "/" + string.Join('/', segments.AsSpan(1)!);

        // If route is exactly just service root, relative becomes "/"
        if (relative == "/") relative = "/";

        var method = context.Request.Method;

        var allowed = await _roleAuthService.CheckAccessAsync(servicePrefix, relative, method, context.User);
        if (!allowed)
        {
            _logger.Warn("Access denied: service={service} path={path} method={method} user={user}",
                servicePrefix, relative, method, context.User.Identity?.Name ?? "anonymous");

            // Return 403 Forbidden with JSON response
            var response = new
            {
                success = false,
                message = "Access Denied: You do not have permission to access this resource",
                statusCode = StatusCodes.Status403Forbidden,
            };
            
            // Set response status code and content type
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(response);
            return;
        }

        await _next(context);
    }
    
    /// <summary>
    /// Determine if the request path should skip authorization
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    private bool ShouldSkipAuthorization(string path)
    {
        var skipPaths = new[]
        {
            "/health",
            "/metrics",
            "/.well-known"
        };
        
        if (path.Contains("swagger", StringComparison.OrdinalIgnoreCase))
            return true;

        // Return true if the path starts with any of the skip paths
        return skipPaths.Any(skipPath =>
            path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase));
    }
    
}