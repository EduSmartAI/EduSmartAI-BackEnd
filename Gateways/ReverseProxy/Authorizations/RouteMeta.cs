namespace ReverseProxy.Authorizations;

public class RouteMeta
{
    public string ServicePrefix { get; set; } = "";
    public string[] AllowedRoles { get; init; } = [];
    public Dictionary<string, string[]> Exceptions { get; init; } = new();
}