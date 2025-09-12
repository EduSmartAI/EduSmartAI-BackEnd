using BuildingBlocks.Exceptions.Handler;
using Course.API.Extensions;

namespace Course.API
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
			//services.AddMapsterConfig();
			//services.AddAuthentication();
			//services.AddAuthorization();

			services.AddExceptionHandler<CustomExceptionHandler>();
			services.AddProblemDetails();
			return services;
		}

		public static WebApplication UseApiServices(this WebApplication app)
		{
			// Configure the HTTP request pipeline here, e.g., app.UseSwagger(), app.UseAuthorization(), etc.
			app.UsePathBase("/course");

			app.UseExceptionHandler();

			app.UseHttpsRedirection();

			app.UseRouting();

			app.UseCors();

			//app.UseAuthentication();
			app.UseStatusCodePages();
			//app.UseAuthorization();

			app.UseSwagger();
			app.UseSwaggerUI(settings =>
			{
				// Đặt endpoint có base path
				settings.SwaggerEndpoint("/swagger/v1/swagger.json", "Course Service v1");
				settings.RoutePrefix = "swagger"; // => /auth/swagger
			});

			app.MapControllers();
			return app;
		}
	}
}
