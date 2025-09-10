using BaseService.Common.Utils.Const;
using ReverseProxy.Authorizations;
using Yarp.ReverseProxy.Configuration;

namespace ReverseProxy.Configurations;

public static class RouteConfiguration
{
    public static IReadOnlyList<RouteConfig> GetRoutes()
    {
        return new List<RouteConfig>
        {
            new RouteConfig
            {
                RouteId = "authServiceRoute",
                ClusterId = ConstReverseProxy.AuthServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/auth/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", "Anonymous" },
                    { "Exceptions", "" }
                }
            },
            
            new RouteConfig
            {
                RouteId = "studentServiceRoute",
                ClusterId = ConstReverseProxy.StudentServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/student/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", ConstRole.Student },
                    { "Exceptions", "" },
                    { nameof(RouteMeta.EndpointRules), 
                    "GET:/api/v1/SelectMajors=Anonymous;"}
                }
            },
            new RouteConfig
            {
                RouteId = "quizServiceRoute",
                ClusterId = ConstReverseProxy.QuizServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/quiz/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", $"{ConstRole.Admin},{ConstRole.Student}" },
                    { "Exceptions", "" }
                }
            },
            new RouteConfig
            {
                RouteId = "teacherServiceRoute",
                ClusterId = ConstReverseProxy.TeacherServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/teacher/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", ConstRole.Lecturer },
                    { "Exceptions", "" },
                }
            },
            new RouteConfig
            {
                RouteId = "courseServiceRoute",
                ClusterId = ConstReverseProxy.CourseServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/course/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", $"{ConstRole.Lecturer},{ConstRole.Student}" },
                    { "Exceptions", "" }
                }
            },
            new RouteConfig
            {
                RouteId = "paymentServiceRoute",
                ClusterId = ConstReverseProxy.PaymentServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/payment/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", ConstRole.Student + "," + ConstRole.Admin },
                    { "Exceptions", "" }
                }
            },
            new RouteConfig
            {
                RouteId = "notificationServiceRoute",
                ClusterId = ConstReverseProxy.NotificationServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/notification/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", ConstRole.Student + "," + ConstRole.Lecturer + "," + ConstRole.Admin },
                    { "Exceptions", "" }
                }
            },
            new RouteConfig
            {
                RouteId = "aiServiceRoute",
                ClusterId = ConstReverseProxy.AiServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/ai/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "RequestHeaderOriginalHost", "true" } },
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", ConstRole.Student + "," + ConstRole.Lecturer },
                    { "Exceptions", "" }
                }
            },
            new RouteConfig
            {
                RouteId = "utilityServiceRoute",
                ClusterId = ConstReverseProxy.UtilityServiceClusterId,
                Match = new RouteMatch
                {
                    Path = "/utility/{**catch-all}",
                },
                Transforms =
                [
                    new Dictionary<string, string> { { "PathRemovePrefix", "/utility" } }
                ],
                Metadata = new Dictionary<string, string>
                {
                    { "AllowedRoles", "Anonymous" },
                    { "Exceptions", "" }
                }
            },
        };
    }
}