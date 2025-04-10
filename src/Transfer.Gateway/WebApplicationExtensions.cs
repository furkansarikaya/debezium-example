using Microsoft.Extensions.Options;
using Transfer.Gateway.Extensions; // For ReverseProxyDocumentFilterConfig

namespace Transfer.Gateway;

public static class WebApplicationExtensions
{
    public static void ConfigurePipeline(this WebApplication app)
    {
        // CORS
        app.UseCors("AllowAll");

        // Request Logging
        app.Use(async (context, next) =>
        {
            // Use LoggerFactory to create a logger instance if needed here
            var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("RequestPipeline"); // Or specific category
            logger.LogDebug("Request Path: {Path}", context.Request.Path);
            await next.Invoke();
        });

        // Swagger UI
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            // Consider injecting IOptions<ReverseProxyDocumentFilterConfig> if this class had dependencies
            var config = app.Services.GetRequiredService<IOptions<ReverseProxyDocumentFilterConfig>>().Value;
            foreach (var cluster in config.Clusters)
            {
                options.SwaggerEndpoint($"/swagger/{cluster.Key}/swagger.json", cluster.Key);
            }
        });
    }

    public static void MapApplicationEndpoints(this WebApplication app)
    {
        app.MapControllers();
        app.MapReverseProxy();
    }
}
