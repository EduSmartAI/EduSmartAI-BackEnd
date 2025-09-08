namespace Course.API
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddApiServices(this IServiceCollection services)
		{
			// Add API services here, e.g., controllers, Swagger, etc.
			return services;
		}

		public static WebApplication UseApiServices(this WebApplication app)
		{
			// Configure the HTTP request pipeline here, e.g., app.UseSwagger(), app.UseAuthorization(), etc.


			return app;
		}
	}
}
