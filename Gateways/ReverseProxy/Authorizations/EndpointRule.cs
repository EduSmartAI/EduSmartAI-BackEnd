namespace ReverseProxy.Authorizations;

public class EndpointRule
{
    public string Method { get; set; } = string.Empty;
    public string PathPattern { get; set; } = string.Empty;
    public string[] Roles { get; set; } = [];
}