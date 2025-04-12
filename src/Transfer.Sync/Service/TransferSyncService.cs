using System.Text.Json;
using Confluent.Kafka;
using Transfer.Shared.Infrastructure;
using Transfer.Shared.Models;

namespace Transfer.Sync.Service;

public class TransferSyncService : BackgroundService
{
    private readonly RedisService _redisService;
    private readonly IConsumer<Ignore, string> _consumer;
    private readonly ILogger<TransferSyncService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _topicName;

    public TransferSyncService(RedisService redisService, ILogger<TransferSyncService> logger, IConfiguration configuration)
    {
        _redisService = redisService;
        _logger = logger;
        _configuration = configuration;
        _topicName = _configuration["KafkaSettings:TopicName"] ?? "transfer-events";

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092",
            GroupId = _configuration["KafkaSettings:GroupId"] ?? "transfer-sync-group",
            AutoOffsetReset = Enum.TryParse<AutoOffsetReset>(_configuration["KafkaSettings:AutoOffsetReset"], out var offsetReset) 
                ? offsetReset 
                : AutoOffsetReset.Earliest
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Transfer Sync Service başlatıldı...");
        
        // Topic'in var olduğundan emin olalım
        await EnsureTopicExistsAsync();
        
        _consumer.Subscribe(_topicName);
        _logger.LogInformation("{TopicName} topic'ine abone olundu", _topicName);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));
                    if (consumeResult?.Message?.Value != null)
                    {
                        _logger.LogInformation("Kafka mesajı alındı: {Value}", consumeResult.Message.Value);
                        
                        // JSON mesajını doğrudan Debezium modeline deserialize et
                        var debeziumMessage = JsonSerializer.Deserialize<DebeziumMessage>(consumeResult.Message.Value);
                        
                        if (debeziumMessage?.Payload != null)
                        {
                            // Mesajın içeriğine ve operasyon tipine göre işlem yap
                            await ProcessDebeziumMessageAsync(debeziumMessage.Payload);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Kafka'dan mesaj okunurken hata oluştu");
                    await Task.Delay(5000, stoppingToken); // Hata durumunda 5 saniye bekle
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "JSON ayrıştırma hatası: {Message}", ex.Message);
                    await Task.Delay(1000, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Beklenmeyen hata: {Message}", ex.Message);
                    await Task.Delay(5000, stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("İşlem iptal edildi");
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Consumer kapatıldı");
        }
    }
    
    private async Task ProcessDebeziumMessageAsync(DebeziumPayload payload)
    {
        try
        {
            // Operasyon tipini kontrol et
            switch (payload.Op)
            {
                case "c": // Create
                case "r": // Read
                    if (payload.After != null)
                    {
                        // DTO'yu Entity'ye dönüştür
                        var transferEntity = payload.After.ToEntity();
                        
                        await _redisService.SetTransferAsync(transferEntity);
                        _logger.LogInformation("Transfer eklendi (ID: {Id}, Amount: {Amount})", 
                            transferEntity.Id, transferEntity.Amount);
                    }
                    break;
                    
                case "u": // Update
                    if (payload.After != null)
                    {
                        // DTO'yu Entity'ye dönüştür
                        var transferEntity = payload.After.ToEntity();
                        
                        await _redisService.SetTransferAsync(transferEntity);
                        _logger.LogInformation("Transfer güncellendi (ID: {Id}, Amount: {Amount})", 
                            transferEntity.Id, transferEntity.Amount);
                    }
                    break;
                    
                case "d": // Delete
                    if (payload.Before != null)
                    {
                        await _redisService.DeleteTransferAsync(payload.Before.Id);
                        _logger.LogInformation("Transfer silindi (ID: {Id})", payload.Before.Id);
                    }
                    break;
                    
                default:
                    _logger.LogWarning("Bilinmeyen operasyon tipi: {OperationType}", payload.Op);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mesaj işlenirken hata oluştu. Operasyon: {Operation}", payload.Op);
        }
    }
    
    private async Task EnsureTopicExistsAsync()
    {
        try
        {
            var bootstrapServers = _configuration["KafkaSettings:BootstrapServers"] ?? "localhost:9092";
            
            using (var adminClient = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build())
            {
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                var topicExists = metadata.Topics.Any(t => t.Topic.Equals(_topicName, StringComparison.OrdinalIgnoreCase));
                
                if (!topicExists)
                {
                    _logger.LogWarning("{TopicName} topic'i bulunamadı, oluşturulmayı bekleniyor...", _topicName);
                    
                    // Topic'in oluşturulmasını bekle
                    int retryCount = 0;
                    while (!topicExists && retryCount < 60) // 5 dakika boyunca kontrol et (5 saniyede bir)
                    {
                        await Task.Delay(5000);
                        metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
                        topicExists = metadata.Topics.Any(t => t.Topic.Equals(_topicName, StringComparison.OrdinalIgnoreCase));
                        retryCount++;
                        
                        _logger.LogInformation("Topic kontrol ediliyor, deneme: {RetryCount}", retryCount);
                    }
                    
                    if (!topicExists)
                    {
                        _logger.LogError("{TopicName} topic'i oluşturulamadı", _topicName);
                        throw new Exception($"{_topicName} topic'i bulunamadı ve oluşturulamadı. register-connector.sh scriptini çalıştırdığınızdan emin olun.");
                    }
                }
                
                _logger.LogInformation("{TopicName} topic'i bulundu, devam ediliyor", _topicName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Topic kontrolü sırasında hata oluştu");
            throw;
        }
    }
}