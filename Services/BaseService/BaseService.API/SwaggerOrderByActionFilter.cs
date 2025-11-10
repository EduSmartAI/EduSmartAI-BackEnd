using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace BaseService.API;

/// <summary>
/// Sort Swagger operations by HTTP method: POST → GET → PUT/PATCH → DELETE → others
/// </summary>
public class SwaggerOrderByActionFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // Sắp xếp operations trong mỗi path
        foreach (var path in swaggerDoc.Paths)
        {
            var orderedOperations = path.Value.Operations
                .OrderBy(op => GetMethodOrder(op.Key))
                .ToList();

            path.Value.Operations.Clear();
            
            foreach (var operation in orderedOperations)
            {
                path.Value.Operations.Add(operation.Key, operation.Value);
            }
        }

        // Nếu muốn sắp xếp cả paths theo method của operation đầu tiên
        var orderedPaths = swaggerDoc.Paths
            .OrderBy(path => GetFirstMethodOrder(path.Value))
            .ThenBy(p => p.Key)
            .ToDictionary(x => x.Key, x => x.Value);

        swaggerDoc.Paths.Clear();
        foreach (var path in orderedPaths)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }

    private int GetMethodOrder(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Post => 1,
            OperationType.Get => 2,
            OperationType.Put => 3,
            OperationType.Patch => 3,
            OperationType.Delete => 4,
            _ => 5
        };
    }

    private int GetFirstMethodOrder(OpenApiPathItem pathItem)
    {
        if (!pathItem.Operations.Any())
            return 5;

        return pathItem.Operations.Keys
            .Select(GetMethodOrder)
            .Min();
    }
}