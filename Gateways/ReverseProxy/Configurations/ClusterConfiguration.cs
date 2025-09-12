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
        var studentServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.StudentServiceUrl);
        var quizServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.QuizServiceUrl);
        var teacherServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.TeacherServiceUrl);
        //var courseServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.CourseServiceUrl);
        var paymentServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.PaymentServiceUrl);
        var notificationServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.NotificationServiceUrl);
        //var aiServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.AiServiceUrl);
        var utilityServiceUrl = Environment.GetEnvironmentVariable(ConstEnv.UtilityServiceUrl);

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
                ClusterId = ConstReverseProxy.StudentServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination2", new DestinationConfig { Address = studentServiceUrl! } }
                }
            },
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.QuizServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination3", new DestinationConfig { Address = quizServiceUrl! } }
                }
            },
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.TeacherServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination4", new DestinationConfig { Address = teacherServiceUrl! } }
                }
            },
            // new ClusterConfig
            // {
            //     ClusterId = ConstReverseProxy.CourseServiceClusterId,
            //     Destinations = new Dictionary<string, DestinationConfig>
            //     {
            //         { "destination5", new DestinationConfig { Address = courseServiceUrl! } }
            //     }
            // },
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.PaymentServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination6", new DestinationConfig { Address = paymentServiceUrl! } }
                }
            },
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.NotificationServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination7", new DestinationConfig { Address = notificationServiceUrl! } }
                }
            },
            // new ClusterConfig
            // {
            //     ClusterId = ConstReverseProxy.AiServiceClusterId,
            //     Destinations = new Dictionary<string, DestinationConfig>
            //     {
            //         {
            //             "destination8", new DestinationConfig { Address = aiServiceUrl! } 
            //         }
            //     }
            // },
            new ClusterConfig
            {
                ClusterId = ConstReverseProxy.UtilityServiceClusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    { "destination9", new DestinationConfig { Address = utilityServiceUrl! } }
                }
            },
        };
    }
}
