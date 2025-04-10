using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Concurrent;

namespace Transfer.Gateway.Extensions
{
    public static class SwaggerExtensions
    {
        public static IReverseProxyBuilder AddSwagger(this IReverseProxyBuilder builder, IConfiguration configuration)
        {
            // Configure reverse proxy filter config
            var config = GetSwaggerConfig(configuration);
            
            // Register config and services
            builder.Services.AddSingleton(Options.Create(config));
            builder.Services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
            builder.Services.AddSwaggerGen();
            
            return builder;
        }
        
        private static ReverseProxyDocumentFilterConfig GetSwaggerConfig(IConfiguration configuration)
        {
            var result = new ReverseProxyDocumentFilterConfig();
            
            // Get routes
            var routesSection = configuration.GetSection("Routes");
            if (routesSection.Exists())
            {
                var routes = new Dictionary<string, ReverseProxyDocumentFilterConfig.Route>();
                
                foreach (var route in routesSection.GetChildren())
                {
                    var routeId = route.Key;
                    var clusterId = route.GetValue<string>("ClusterId");
                    
                    if (!string.IsNullOrEmpty(clusterId))
                    {
                        routes[routeId] = new ReverseProxyDocumentFilterConfig.Route
                        {
                            RouteId = routeId,
                            ClusterId = clusterId
                        };
                    }
                }
                
                result.Routes = routes;
            }
            
            // Get clusters
            var clustersSection = configuration.GetSection("Clusters");
            if (clustersSection.Exists())
            {
                var clusters = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster>();
                
                foreach (var cluster in clustersSection.GetChildren())
                {
                    var clusterId = cluster.Key;
                    var destinationsSection = cluster.GetSection("Destinations");
                    
                    if (destinationsSection.Exists())
                    {
                        var destinations = new Dictionary<string, ReverseProxyDocumentFilterConfig.Cluster.Destination>();
                        
                        foreach (var destination in destinationsSection.GetChildren())
                        {
                            var destinationId = destination.Key;
                            var address = destination.GetValue<string>("Address");
                            
                            if (!string.IsNullOrEmpty(address))
                            {
                                var swaggersSection = destination.GetSection("Swaggers");
                                var swaggers = new List<ReverseProxyDocumentFilterConfig.Cluster.Destination.Swagger>();
                                
                                if (swaggersSection.Exists())
                                {
                                    foreach (var swagger in swaggersSection.GetChildren())
                                    {
                                        var prefixPath = swagger.GetValue<string>("PrefixPath");
                                        var paths = swagger.GetSection("Paths").Get<List<string>>() ?? new List<string>();
                                        
                                        if (!string.IsNullOrEmpty(prefixPath) && paths.Any())
                                        {
                                            swaggers.Add(new ReverseProxyDocumentFilterConfig.Cluster.Destination.Swagger
                                            {
                                                PrefixPath = prefixPath,
                                                Paths = paths.ToArray()
                                            });
                                        }
                                    }
                                }
                                
                                destinations[destinationId] = new ReverseProxyDocumentFilterConfig.Cluster.Destination
                                {
                                    Address = address,
                                    Swaggers = swaggers.ToArray()
                                };
                            }
                        }
                        
                        clusters[clusterId] = new ReverseProxyDocumentFilterConfig.Cluster
                        {
                            Destinations = destinations
                        };
                    }
                }
                
                result.Clusters = clusters;
            }
            
            return result;
        }
    }
    
    public class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
    {
        private readonly ReverseProxyDocumentFilterConfig _config;

        public ConfigureSwaggerOptions(IOptions<ReverseProxyDocumentFilterConfig> config)
        {
            _config = config.Value;
        }

        public void Configure(SwaggerGenOptions options)
        {
            foreach (var cluster in _config.Clusters)
            {
                options.SwaggerDoc(cluster.Key, new OpenApiInfo { Title = cluster.Key, Version = "v1" });
            }

            options.DocumentFilter<ReverseProxyDocumentFilter>();
        }
    }
    
    public class ReverseProxyDocumentFilter : IDocumentFilter
    {
        private readonly ReverseProxyDocumentFilterConfig _config;
        private readonly IHttpClientFactory _httpClientFactory;
        private static readonly ConcurrentDictionary<string, OpenApiDocument> _documentCache = new();

        public ReverseProxyDocumentFilter(IOptions<ReverseProxyDocumentFilterConfig> config, IHttpClientFactory httpClientFactory)
        {
            _config = config.Value;
            _httpClientFactory = httpClientFactory;
        }

        public async void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            // Get the cluster for this document
            var clusterId = context.DocumentName;
            if (!_config.Clusters.TryGetValue(clusterId, out var cluster))
            {
                return;
            }
            
            swaggerDoc.Paths = new OpenApiPaths();
            
            // Initialize components
            swaggerDoc.Components ??= new OpenApiComponents();
            swaggerDoc.Components.Schemas ??= new Dictionary<string, OpenApiSchema>();
            
            // Process each destination and its swagger files
            foreach (var destination in cluster.Destinations.Values)
            {
                foreach (var swagger in destination.Swaggers)
                {
                    foreach (var path in swagger.Paths)
                    {
                        try
                        {
                            // Get the full URL to the swagger file
                            var swaggerUrl = $"{destination.Address.TrimEnd('/')}{path}";
                            
                            // Get or create the swagger document
                            var apiDoc = await GetOrCreateSwaggerDocument(swaggerUrl);
                            if (apiDoc == null) continue;
                            
                            // Copy all components (schemas)
                            if (apiDoc.Components?.Schemas != null)
                            {
                                foreach (var schema in apiDoc.Components.Schemas)
                                {
                                    if (!swaggerDoc.Components.Schemas.ContainsKey(schema.Key))
                                    {
                                        swaggerDoc.Components.Schemas.Add(schema.Key, schema.Value);
                                    }
                                }
                            }
                            
                            // Apply the prefix to paths
                            var prefix = swagger.PrefixPath;
                            foreach (var swaggerPath in apiDoc.Paths)
                            {
                                var newPath = swaggerPath.Key;
                                if (!string.IsNullOrEmpty(prefix))
                                {
                                    newPath = $"{prefix.TrimEnd('/')}{newPath}";
                                }
                                
                                // Add to the main document
                                if (!swaggerDoc.Paths.ContainsKey(newPath))
                                {
                                    swaggerDoc.Paths[newPath] = swaggerPath.Value;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing swagger document: {ex.Message}");
                        }
                    }
                }
            }
        }
        
        private async Task<OpenApiDocument?> GetOrCreateSwaggerDocument(string url)
        {
            if (_documentCache.TryGetValue(url, out var cachedDocument))
            {
                return cachedDocument;
            }
            
            try
            {
                using var client = _httpClientFactory.CreateClient();
                using var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                
                using var stream = await response.Content.ReadAsStreamAsync();
                var document = new OpenApiStreamReader().Read(stream, out _);
                
                _documentCache[url] = document;
                return document;
            }
            catch
            {
                return null;
            }
        }
    }
    
    public class ReverseProxyDocumentFilterConfig
    {
        public Dictionary<string, Route> Routes { get; set; } = new();
        public Dictionary<string, Cluster> Clusters { get; set; } = new();
        
        public class Route
        {
            public string RouteId { get; set; } = string.Empty;
            public string ClusterId { get; set; } = string.Empty;
        }
        
        public class Cluster
        {
            public Dictionary<string, Destination> Destinations { get; set; } = new();
            
            public class Destination
            {
                public string Address { get; set; } = string.Empty;
                public Swagger[] Swaggers { get; set; } = Array.Empty<Swagger>();
                
                public class Swagger
                {
                    public string PrefixPath { get; set; } = string.Empty;
                    public string[] Paths { get; set; } = Array.Empty<string>();
                }
            }
        }
    }
} 