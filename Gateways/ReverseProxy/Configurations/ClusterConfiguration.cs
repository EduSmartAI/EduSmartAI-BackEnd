using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using Yarp.ReverseProxy.Configuration;

namespace ReverseProxy.Configurations;

public static class ClusterConfiguration
{
    public static IReadOnlyList<ClusterConfig> GetClusters()
    {
        EnvLoader.Load();
        var authServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.AuthServiceUrl);
        var studentServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.UserServiceUrl);
        
        return new List<ClusterConfig>
        {
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.AuthServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination1", new DestinationConfig { Address = authServiceUrl! } },
                }
            },
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.UserServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination2", new DestinationConfig { Address = studentServiceUrl! } }
                }
            }
        };
    }
}
