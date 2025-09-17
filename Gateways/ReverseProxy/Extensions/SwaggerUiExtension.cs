namespace ReverseProxy.Extensions;

public static class SwaggerUiExtension
{
    /// <summary>
    /// Configure Swagger UI for Reverse Proxy
    /// </summary>
    public static void ConfigureSwaggerUi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            
            c.SwaggerEndpoint("/api/swagger/aggregated", "TẤT CẢ API - TỔNG HỢP");
            
            c.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "Auth Service");
            c.SwaggerEndpoint("/student/swagger/v1/swagger.json", "Student Service");
            c.SwaggerEndpoint("/teacher/swagger/v1/swagger.json", "Teacher Service");
            c.SwaggerEndpoint("/course/swagger/v1/swagger.json", "Course Service");
            c.SwaggerEndpoint("/quiz/swagger/v1/swagger.json", "Quiz Service");
            c.SwaggerEndpoint("/utility/swagger/v1/swagger.json", "Utility Service");
            c.SwaggerEndpoint("/payment/swagger/v1/swagger.json", "Payment Service");
            c.SwaggerEndpoint("/notification/swagger/v1/swagger.json", "Notification Service");
            
            c.DocumentTitle = "EduSmart API Gateway - Swagger UI";
            c.DefaultModelsExpandDepth(-1);
            c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
            c.EnableDeepLinking();
            c.EnableFilter();
        });
    }
}