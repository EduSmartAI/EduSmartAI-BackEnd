using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;

namespace ReverseProxy.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SwaggerController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SwaggerController> _logger;

    public SwaggerController(HttpClient httpClient, ILogger<SwaggerController> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Get all Swagger specs from services and aggregate them into a single spec
    /// </summary>
    [HttpGet("aggregated")]
    public async Task<IActionResult> GetAggregatedSwagger()
    {
        EnvLoader.Load();
        try
        {
            _logger.LogInformation("Starting to create aggregated Swagger spec...");
            
            var serviceEndpoints = new Dictionary<string, string>
            {
                { "Auth Service", "/auth/swagger/v1/swagger.json" },
                { "Student Service", "/student/swagger/v1/swagger.json" },
                { "Teacher Service", "/teacher/swagger/v1/swagger.json" },
                { "Course Service", "/course/swagger/v1/swagger.json" },
                { "Quiz Service", "/quiz/swagger/v1/swagger.json" },
                { "Utility Service", "/utility/swagger/v1/swagger.json" },
                { "Payment Service", "/payment/swagger/v1/swagger.json" },
                { "Notification Service", "/notification/swagger/v1/swagger.json" },
                { "AI Service", "/ai/swagger/v1/swagger.json" },
            };

            var allPaths = new Dictionary<string, object>();
            var allSchemas = new Dictionary<string, object>();
            var allSecuritySchemes = new Dictionary<string, object>();
            var allTags = new List<object>();
            var allServers = new List<object>();

            var baseUrl = Environment.GetEnvironmentVariable(ConstEnv.WebsiteDomain) ?? Environment.GetEnvironmentVariable(ConstEnv.ReverseProxyUrl);
            _logger.LogInformation($"Gateway Base URL: {baseUrl}");

            // Determine the public domain for Swagger UI
            _logger.LogInformation($"Using public domain for Swagger: {baseUrl}");
            
            // Add public domain server first
            allServers.Add(new { url = baseUrl, description = "EduSmart API Server" });

            // Get specs from each service and aggregate them
            foreach (var service in serviceEndpoints)
            {
                try
                {
                    var swaggerUrl = baseUrl + service.Value;
                    _logger.LogInformation($"Getting Swagger spec from: {swaggerUrl}");
                    
                    var response = await _httpClient.GetAsync(swaggerUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonContent = await response.Content.ReadAsStringAsync();
                        var serviceSpec = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                        // Add tag for service
                        allTags.Add(new
                        {
                            name = service.Key,
                            description = $"API endpoints from {service.Key}"
                        });

                        // Handle both Swagger 2.0 and OpenAPI 3.0 specs
                        if (serviceSpec.TryGetProperty("paths", out var pathsElement))
                        {
                            // Check if it's Swagger 2.0 format
                            if (serviceSpec.TryGetProperty("swagger", out var swaggerVersion) && 
                                swaggerVersion.GetString() == "2.0")
                            {
                                // Swagger 2.0 format - get basePath
                                var basePath = "";
                                if (serviceSpec.TryGetProperty("basePath", out var basePathElement))
                                {
                                    basePath = basePathElement.GetString() ?? "";
                                }

                                foreach (var path in pathsElement.EnumerateObject())
                                {
                                    // Combine basePath with path for full URL
                                    var fullPath = basePath + path.Name;
                                    var pathValue = path.Value;

                                    if (pathValue.ValueKind == JsonValueKind.Object)
                                    {
                                        var modifiedPath = ModifyPathWithServiceTag(pathValue, service.Key);
                                        allPaths[fullPath] = modifiedPath;
                                    }
                                }
                            }
                            else
                            {
                                // OpenAPI 3.0 format
                                foreach (var path in pathsElement.EnumerateObject())
                                {
                                    var pathKey = path.Name;
                                    var pathValue = path.Value;

                                    if (pathValue.ValueKind == JsonValueKind.Object)
                                    {
                                        var modifiedPath = ModifyPathWithServiceTag(pathValue, service.Key);
                                        allPaths[pathKey] = modifiedPath;
                                    }
                                }
                            }
                        }

                        // Aggregate schemas from components (OpenAPI 3.0) or definitions (Swagger 2.0)
                        if (serviceSpec.TryGetProperty("components", out var components))
                        {
                            if (components.TryGetProperty("schemas", out var schemas))
                            {
                                foreach (var schema in schemas.EnumerateObject())
                                {
                                    var schemaKey = $"{service.Key.Replace(" ", "")}_{schema.Name}";
                                    allSchemas[schemaKey] = schema.Value;
                                }
                            }

                            // Aggregate security schemes
                            if (components.TryGetProperty("securitySchemes", out var securitySchemes))
                            {
                                foreach (var scheme in securitySchemes.EnumerateObject())
                                {
                                    allSecuritySchemes[scheme.Name] = scheme.Value;
                                }
                            }
                        }
                        else if (serviceSpec.TryGetProperty("definitions", out var definitions))
                        {
                            // Swagger 2.0 definitions
                            foreach (var definition in definitions.EnumerateObject())
                            {
                                var schemaKey = $"{service.Key.Replace(" ", "")}_{definition.Name}";
                                allSchemas[schemaKey] = definition.Value;
                            }
                        }
                        
                        _logger.LogInformation($"Successfully retrieved spec from {service.Key}");
                    }
                    else
                    {
                        _logger.LogWarning($"Unable to retrieve spec from {service.Key}: {response.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error retrieving Swagger spec from {service.Key}: {ex.Message}");
                }
            }

            // If no paths found, create a default response
            if (!allPaths.Any())
            {
                _logger.LogWarning("No APIs found from any services");
                allPaths.Add("/api/gateway/health", new
                {
                    get = new
                    {
                        tags = new[] { "Gateway" },
                        summary = "Gateway Health Check",
                        description = "Check the status of the Gateway",
                        responses = new Dictionary<string, object>
                        {
                            ["200"] = new
                            {
                                description = "Gateway is running",
                                content = new Dictionary<string, object>
                                {
                                    ["application/json"] = new
                                    {
                                        schema = new
                                        {
                                            type = "object",
                                            properties = new Dictionary<string, object>
                                            {
                                                ["status"] = new { type = "string" },
                                                ["message"] = new { type = "string" }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                });
                
                allTags.Add(new
                {
                    name = "Gateway", 
                    description = "Gateway API endpoints"
                });
            }

            // Create final aggregated spec
            var finalSpec = new
            {
                openapi = "3.0.1",
                info = new
                {
                    title = "EduSmart API Gateway - All Services",
                    version = "v1",
                    description = $"API Gateway aggregating all services in EduSmart system. Total {allPaths.Count} endpoints from {allTags.Count} services."
                },
                servers = allServers,
                paths = allPaths,
                components = new
                {
                    schemas = allSchemas,
                    securitySchemes = allSecuritySchemes
                },
                tags = allTags
            };

            _logger.LogInformation($"Completed creating aggregated Swagger spec: {allPaths.Count} endpoints, {allTags.Count} services");
            return Ok(finalSpec);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Critical error creating aggregated Swagger spec: {ex.Message}");
            return StatusCode(500, new { 
                error = "Error creating aggregated Swagger spec", 
                details = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    private object ModifyPathWithServiceTag(JsonElement pathValue, string serviceTag)
    {
        var pathDict = new Dictionary<string, object>();

        foreach (var method in pathValue.EnumerateObject())
        {
            if (method.Value.ValueKind == JsonValueKind.Object)
            {
                var methodDict = new Dictionary<string, object>();
                
                foreach (var prop in method.Value.EnumerateObject())
                {
                    if (prop.Name == "tags")
                    {
                        // Replace tags with service tag
                        methodDict[prop.Name] = new[] { serviceTag };
                    }
                    else
                    {
                        var deserializedValue = JsonSerializer.Deserialize<object>(prop.Value.GetRawText());
                        if (deserializedValue != null)
                        {
                            methodDict[prop.Name] = deserializedValue;
                        }
                    }
                }

                // If no tags exist, add service tag
                if (!methodDict.ContainsKey("tags"))
                {
                    methodDict["tags"] = new[] { serviceTag };
                }

                pathDict[method.Name] = methodDict;
            }
        }

        return pathDict;
    }
}
