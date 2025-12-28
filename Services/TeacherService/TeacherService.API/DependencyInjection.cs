using Microsoft.OpenApi;
using TeacherService.API.Extensions;

namespace TeacherService.API
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
			app.UsePathBase("/teacher");

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
				settings.RoutePrefix = "swagger";
			});

			app.MapControllers();

			return app;
		}
	}
}
