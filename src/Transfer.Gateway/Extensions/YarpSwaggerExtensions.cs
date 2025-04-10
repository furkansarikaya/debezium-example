using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;

namespace Transfer.Gateway.Extensions
{
    public static class YarpSwaggerExtensions
    {
        // Swagger yapılandırması için sınıf
        public class SwaggerConfig
        {
            public string Endpoint { get; set; } = string.Empty;
            public string Spec { get; set; } = string.Empty;
            public string OriginPath { get; set; } = string.Empty;
            public string TargetPath { get; set; } = string.Empty;
        }

        // SwaggerUI için endpointler
        public class SwaggerEndpoint
        {
            public string Name { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        // SwaggerUI yapılandırması
        public class SwaggerUIConfig
        {
            public string RoutePrefix { get; set; } = "docs";
            public List<SwaggerEndpoint> Endpoints { get; set; } = new List<SwaggerEndpoint>();
        }

        // Gateway uygula ve Swagger UI'ı yapılandır
        public static void ConfigureGateway(this WebApplication app, IConfiguration configuration)
        {
            // SwaggerUI yapılandırmasını al
            var swaggerUIConfig = configuration.GetSection("Gateway:SwaggerUI").Get<SwaggerUIConfig>() ?? new SwaggerUIConfig();

            // Swagger UI'ı yapılandır
            app.UseSwagger(c =>
            {
                c.RouteTemplate = $"{swaggerUIConfig.RoutePrefix}/{{documentName}}/swagger.json";
            });

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/docs/v1/swagger.json", "Transfer Gateway v1");
                // SwaggerUI endpointlerini yapılandırmadan al
                foreach (var endpoint in swaggerUIConfig.Endpoints)
                {
                    c.SwaggerEndpoint(endpoint.Url, endpoint.Name);
                }

                c.RoutePrefix = swaggerUIConfig.RoutePrefix;
                c.EnableDeepLinking();
                c.DisplayOperationId();
            });

            // Direk swagger.json endpointleri için handler ekle
            RegisterSwaggerJsonHandlers(app, configuration);

            // Controllers ve proxy
            app.MapControllers();
            app.MapReverseProxy();
        }

        // Swagger JSON endpointleri için özel handler'ları kaydeder
        private static void RegisterSwaggerJsonHandlers(WebApplication app, IConfiguration configuration)
        {
            var clusters = configuration.GetSection("ReverseProxy:Clusters");
            
            if (clusters != null)
            {
                foreach (var cluster in clusters.GetChildren())
                {
                    var swaggerConfig = cluster.GetSection("Swagger").Get<SwaggerConfig>();
                    
                    if (swaggerConfig != null && !string.IsNullOrEmpty(swaggerConfig.Endpoint) && !string.IsNullOrEmpty(swaggerConfig.Spec))
                    {
                        // Doğrudan swagger.json sonucunu döndürecek bir handler ekle
                        app.MapGet(swaggerConfig.Endpoint, async (HttpContext context) =>
                        {
                            return await FetchAndTransformSwaggerJson(swaggerConfig, context);
                        });
                    }
                }
            }
        }
        
        // Swagger JSON'ı alıp dönüştürür
        private static async Task<IResult> FetchAndTransformSwaggerJson(SwaggerConfig config, HttpContext context)
        {
            try
            {
                using var client = new HttpClient();
                var stream = await client.GetStreamAsync(config.Spec);
                
                var document = new OpenApiStreamReader().Read(stream, out var diagnostic);
                
                // API yollarını gateway formatına dönüştür
                var rewrittenPaths = new OpenApiPaths();
                
                foreach (var path in document.Paths)
                {
                    // Gateway üzerinden erişim için path'i düzenle
                    // Burada path dönüşümünü yapılandırmadan al
                    string rewrittenPath = path.Key;
                    
                    if (!string.IsNullOrEmpty(config.TargetPath) && !string.IsNullOrEmpty(config.OriginPath))
                    {
                        rewrittenPath = path.Key.Replace(config.TargetPath, config.OriginPath);
                    }
                    
                    rewrittenPaths.Add(rewrittenPath, path.Value);
                }
                
                document.Paths = rewrittenPaths;
                
                // JSON olarak serileştir ve yanıt olarak gönder
                using var memoryStream = new MemoryStream();
                using var writer = new StreamWriter(memoryStream, leaveOpen: true);
                var jsonWriter = new OpenApiJsonWriter(writer);
                document.SerializeAsV3(jsonWriter);
                await writer.FlushAsync();
                
                memoryStream.Position = 0;
                
                // Stream'den string oluştur
                using var reader = new StreamReader(memoryStream);
                var json = await reader.ReadToEndAsync();
                
                return Results.Content(json, "application/json");
            }
            catch (Exception ex)
            {
                return Results.Problem($"Error retrieving Swagger from {config.Spec}: {ex.Message}", statusCode: 500);
            }
        }
    }
} 