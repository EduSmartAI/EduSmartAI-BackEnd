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
            c.SwaggerEndpoint("/auth/swagger/v1/swagger.json", "Auth Service Swagger");
            c.SwaggerEndpoint("/student/swagger/v1/swagger.json", "Student Service Swagger");
            c.SwaggerEndpoint("/teacher/swagger/v1/swagger.json", "Teacher Service Swagger");
            c.SwaggerEndpoint("/course/swagger/v1/swagger.json", "Course Service Swagger");
            c.SwaggerEndpoint("/quiz/swagger/v1/swagger.json", "Quiz Service Swagger");
            c.SwaggerEndpoint("/utility/swagger/v1/swagger.json", "Util Service Swagger");
            c.SwaggerEndpoint("/ai/swagger/v1/swagger.json", "AI Service Swagger");
            c.SwaggerEndpoint("/payment/swagger/v1/swagger.json", "Payment Service Swagger");
            c.SwaggerEndpoint("/notification/swagger/v1/swagger.json", "Notification Service Swagger");
        });
    }
}