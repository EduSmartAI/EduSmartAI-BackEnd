using Course.API.Extensions;

namespace Course.API
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApiServices(this IServiceCollection services)
		{
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
			app.UsePathBase("/course");

			app.UseExceptionHandler();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors();

			app.UseAuthentication();

			app.UseStatusCodePages();

			app.UseAuthorization();

			app.UseSwagger();
			app.UseSwaggerUI(settings =>
			{
				settings.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Service v1");
				settings.RoutePrefix = "swagger";
			});

			app.MapControllers();
			return app;
		}
	}
}
