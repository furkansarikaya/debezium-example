using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using System.Text.Json;
using Transfer.Shared.Models;

namespace Transfer.Shared.Infrastructure;

public class RedisService
{
    private readonly IDatabase _redis;

    public RedisService(IConfiguration configuration)
    {
        var connectionString = $"{configuration["RedisSettings:Host"]}:{configuration["RedisSettings:Port"]},password={configuration["RedisSettings:Password"]},abortConnect=false";
        var redis = ConnectionMultiplexer.Connect(connectionString);
        _redis = redis.GetDatabase();
    }

    public async Task SetTransferAsync(TransferEntity transfer)
    {
        var key = $"transfer:{transfer.Id}";
        var value = JsonSerializer.Serialize(transfer);
        await _redis.StringSetAsync(key, value);
        await _redis.SetAddAsync("transfers", key);
    }

    public async Task<TransferEntity?> GetTransferAsync(int id)
    {
        var key = $"transfer:{id}";
        var value = await _redis.StringGetAsync(key);
        return value.HasValue ? JsonSerializer.Deserialize<TransferEntity>(value!) : null;
    }

    public async Task<List<TransferEntity>> GetAllTransfersAsync()
    {
        var keys = await _redis.SetMembersAsync("transfers");
        var transfers = new List<TransferEntity>();

        foreach (var key in keys)
        {
            var value = await _redis.StringGetAsync(key.ToString());
            if (value.HasValue)
            {
                var transfer = JsonSerializer.Deserialize<TransferEntity>(value!);
                if (transfer != null)
                {
                    transfers.Add(transfer);
                }
            }
        }

        return transfers;
    }
    
    /// <summary>
    /// Belirtilen ID'ye sahip transfer kaydını Redis'ten siler.
    /// </summary>
    /// <param name="id">Silinecek transfer kaydının ID'si</param>
    public async Task DeleteTransferAsync(int id)
    {
        var key = $"transfer:{id}";
        
        // Önce anahtarı sil
        await _redis.KeyDeleteAsync(key);
        
        // Sonra "transfers" setinden de çıkar
        await _redis.SetRemoveAsync("transfers", key);
    }
    
    /// <summary>
    /// Belirtilen anahtarı Redis'ten siler.
    /// </summary>
    /// <param name="key">Silinecek anahtar</param>
    public async Task DeleteKeyAsync(string key)
    {
        await _redis.KeyDeleteAsync(key);
    }
} 