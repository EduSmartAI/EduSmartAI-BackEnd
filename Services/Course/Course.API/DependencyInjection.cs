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
			services.AddAuthentication();
			services.AddAuthorization();
			return services;
		}

		public static WebApplication UseApiServices(this WebApplication app)
		{
			// Configure the HTTP request pipeline here, e.g., app.UseSwagger(), app.UseAuthorization(), etc.

			app.UseCors();
			app.UsePathBase("/auth");
			app.UseRouting();
			app.UseAuthentication();
			app.UseStatusCodePages();
			app.UseAuthorization();
			app.UseHttpsRedirection();
			app.MapControllers();
			app.UseSwagger();
			app.UseSwaggerUI(settings =>
			{
				settings.RoutePrefix = "swagger";
			});
			return app;
		}
	}
}
