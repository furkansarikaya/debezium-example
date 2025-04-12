using Transfer.Gateway.Extensions;

namespace Transfer.Gateway;

public static class ServiceCollectionExtensions
{
    public static void ConfigureServices(this WebApplicationBuilder builder)
    {
        // Core Services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHttpClient();

        // Swagger Configuration
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Transfer Gateway API",
                Version = "v1",
                Description = "Gateway API for Transfer services"
            });
        });

        // CORS Configuration
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        // Reverse Proxy & Swagger Integration
        var configuration = builder.Configuration.GetSection("ReverseProxy");
        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(configuration)
            .AddSwagger(configuration);

        // Logging Configuration
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
    }
}
