using Transfer.Shared.Infrastructure;
using Transfer.Sync.Service;

var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureServices((hostContext, services) =>
{
    services.AddHostedService<TransferSyncService>();
    services.AddSingleton<RedisService>();
});

var host = builder.Build();
await host.RunAsync();