using Microsoft.OpenApi;
using PaymentService.API.Extensions;

namespace PaymentService.API
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApiServices(this IServiceCollection services)
		{
			// Add API services here, e.g., controllers, Swagger, etc.
			services.AddControllers();
			services.AddEndpointsApiExplorer();
			services.AddSwaggerServices();
			services.AddCorsServices();
			services.AddMessagingServices();
			services.AddAuthenticationServices();
			services.AddAuthorization();
			services.AddProblemDetails();
			return services;
		}

		public static WebApplication UseApiServices(this WebApplication app)
		{
			// Configure the HTTP request pipeline here, e.g., app.UseSwagger(), app.UseAuthorization(), etc.
			app.UsePathBase("/payment");

			app.UseExceptionHandler();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors();

			app.UseAuthentication();

			app.UseStatusCodePages();

			app.UseAuthorization();

			app.UseSwagger(c => c.OpenApiVersion = OpenApiSpecVersion.OpenApi2_0);
			app.UseSwaggerUI(settings =>
			{
				settings.SwaggerEndpoint("/swagger/v1/swagger.json", "Payment Service v1");
				settings.RoutePrefix = "swagger";
			});

			app.MapControllers();
			return app;
		}
	}
}
