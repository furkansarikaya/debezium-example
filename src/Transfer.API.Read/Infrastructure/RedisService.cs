using StackExchange.Redis;
using System.Text.Json;

namespace Transfer.API.Read.Infrastructure;

public class RedisService
{
    private readonly IDatabase _redis;

    public RedisService(IConfiguration configuration)
    {
        var connectionString = $"{configuration["RedisSettings:Host"]}:{configuration["RedisSettings:Port"]},password={configuration["RedisSettings:Password"]}";
        var redis = ConnectionMultiplexer.Connect(connectionString);
        _redis = redis.GetDatabase();
    }

    public async Task<Features.Transfers.Models.Transfer?> GetTransferAsync(int id)
    {
        var key = $"transfer:{id}";
        var value = await _redis.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<Features.Transfers.Models.Transfer>(value!) : null;
    }

    public async Task<List<Features.Transfers.Models.Transfer>> GetAllTransfersAsync()
    {
        var keys = await _redis.SetMembersAsync("transfers");
        var transfers = new List<Features.Transfers.Models.Transfer>();

        foreach (var key in keys)
        {
            var value = await _redis.StringGetAsync(key.ToString());
            if (value.HasValue)
            {
                var transfer = JsonSerializer.Deserialize<Features.Transfers.Models.Transfer>(value!);
                if (transfer != null)
                {
                    transfers.Add(transfer);
                }
            }
        }

        return transfers;
    }
} 