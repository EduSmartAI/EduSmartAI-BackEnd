using System.Collections.Concurrent;
using System.Security.Claims;
using ReverseProxy.Configurations;

namespace ReverseProxy.Authorizations;

public class RoleAuthorizationService : IRoleAuthorizationService
{
    private readonly ConcurrentDictionary<string, RouteMeta> _map;
    
    /// <summary>
    /// Constructor
    /// </summary>
    public RoleAuthorizationService()
    {
        _map = new ConcurrentDictionary<string, RouteMeta>(StringComparer.OrdinalIgnoreCase);

        // Load from RouteConfiguration class (synchronous OK)
        foreach (var route in RouteConfiguration.GetRoutes())
        {
            // Extract service prefix from route path, allowed roles, exceptions
            var rawPath = route.Match?.Path ?? string.Empty;
            var servicePrefix = ExtractServicePrefix(rawPath);
            var allowedRoles = route.Metadata != null && route.Metadata.TryGetValue("AllowedRoles", out var r) ? r : "";
            var exceptions = route.Metadata != null && route.Metadata.TryGetValue("Exceptions", out var e) ? e : "";

            var meta = new RouteMeta
            {
                ServicePrefix = servicePrefix,
                AllowedRoles = allowedRoles.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray(),
                Exceptions = ParseExceptions(exceptions)
            };

            _map[servicePrefix] = meta;
        }
    }
    
    /// <summary>
    /// Check if user has access to the given service prefix and path
    /// </summary>
    /// <param name="servicePrefix"></param>
    /// <param name="relativePath"></param>
    /// <param name="method"></param>
    /// <param name="user"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> CheckAccessAsync(string servicePrefix, string relativePath, string method, ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var identity = user?.Identity as ClaimsIdentity;
        
        // If service prefix not found, deny by default
        if (!_map.TryGetValue(servicePrefix, out var meta))
        {
            return Task.FromResult(false);
        }

        // Normalize
        relativePath = NormalizePath(relativePath);
        method = method?.ToUpperInvariant() ?? "GET";

        // 1. Check exceptions first, if match => allow
        if (meta.Exceptions.TryGetValue(method, out var paths))
        {
            if (paths.Any(p => PathMatches(p, relativePath)))
                return Task.FromResult(true);
        }

        // 2. If AllowedRoles contains "Anonymous" => public route
        if (meta.AllowedRoles.Any(r => string.Equals(r, "Anonymous", StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(true);

        // 3. If user not authenticated => deny
        if (identity == null || !identity.IsAuthenticated)
            return Task.FromResult(false);

        // 4. Extract user roles from claims
        var roles = user?.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles")
            .Select(c => c.Value)
            .SelectMany(v => v.Split(',', StringSplitOptions.RemoveEmptyEntries))
            .Select(x => x.Trim())
            .ToArray();

        // 5. Check intersection
        var allowed = meta.AllowedRoles.Intersect(roles, StringComparer.OrdinalIgnoreCase).Any();
        return Task.FromResult(allowed);
    }
    
    /// <summary>
    /// Extract service prefix from raw route path
    /// </summary>
    /// <param name="rawPath"></param>
    /// <returns></returns>
    private static string ExtractServicePrefix(string rawPath)
    {
        // rawPath like "/user/{**catch-all}" => take first segment
        var trimmed = rawPath.Trim('/');
        var seg = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return seg.Length > 0 ? seg[0] : "";
    }
    
    /// <summary>
    /// Parse exceptions from raw string to dictionary
    /// </summary>
    /// <param name="raw"></param>
    /// <returns></returns>
    private static Dictionary<string, string[]> ParseExceptions(string raw)
    {
        // raw example: "GET:/public-profile,POST:/notify"
        var dict = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(raw)) return dict;

        // Split by comma, then by colon
        var pairs = raw.Split(',', StringSplitOptions.RemoveEmptyEntries);
        foreach (var p in pairs)
        {
            // Split by first colon
            var parts = p.Split(':', 2);
            
            // If not exactly 2 parts, skip
            if (parts.Length != 2) continue;
            
            // Normalize method and path
            var method = parts[0].Trim().ToUpperInvariant();
            
            // If method is "*", treat as all methods (for simplicity, we can store under key "*")
            var path = NormalizePath(parts[1].Trim());
            if (!dict.TryGetValue(method, out _))
            {
                dict[method] = [path];
            }
            
            // Append to existing list
            else
            {
                dict[method] = dict[method].Concat([path]).ToArray();
            }
        }
        return dict;
    }
    
    /// <summary>
    /// Normalize path: ensure starts with '/', no trailing '/'
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    private static string NormalizePath(string p)
    {
        if (string.IsNullOrEmpty(p)) return "/";
        if (!p.StartsWith("/")) p = "/" + p;
        return p.TrimEnd('/');
    }
    
    /// <summary>
    /// Check if actual path matches the pattern
    /// </summary>
    /// <param name="pattern"></param>
    /// <param name="actual"></param>
    /// <returns></returns>
    private static bool PathMatches(string pattern, string actual)
    {
        // simple exact or prefix match; can extend to wildcard
        if (string.Equals(pattern, actual, StringComparison.OrdinalIgnoreCase)) return true;
        // prefix: pattern "/public" should match "/public/sub"?
        if (actual.StartsWith(pattern + "/", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}