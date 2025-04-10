using Microsoft.Extensions.Options;
using Transfer.Gateway.Extensions;
using Transfer.Gateway;

var builder = WebApplication.CreateBuilder(args);

builder.ConfigureServices();

var app = builder.Build();

app.ConfigurePipeline(); 
app.MapApplicationEndpoints(); 

await app.RunAsync();