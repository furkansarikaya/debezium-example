using Microsoft.Extensions.Options;
using Transfer.Gateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add HttpClientFactory
builder.Services.AddHttpClient();

// Add Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Transfer Gateway API",
        Version = "v1",
        Description = "Gateway API for Transfer services"
    });
});

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Swagger and YARP
var configuration = builder.Configuration.GetSection("ReverseProxy");
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(configuration)
    .AddSwagger(configuration);

// Add logging
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var app = builder.Build();

// Configure the HTTP request pipeline.

// Enable CORS
app.UseCors("AllowAll");

// Logging middleware for debugging route issues
app.Use(async (context, next) =>
{
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogDebug("Request Path: {Path}", context.Request.Path);
    await next.Invoke();
});

// Configure Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    var config = app.Services.GetRequiredService<IOptions<ReverseProxyDocumentFilterConfig>>().Value;
    foreach (var cluster in config.Clusters)
    {
        options.SwaggerEndpoint($"/swagger/{cluster.Key}/swagger.json", cluster.Key);
    }
});

// Map controllers and routes
app.MapControllers();
app.MapReverseProxy();

await app.RunAsync();