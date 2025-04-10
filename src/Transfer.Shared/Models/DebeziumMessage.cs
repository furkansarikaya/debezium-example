using System.Text.Json.Serialization;

namespace Transfer.Shared.Models;

/// <summary>
/// Debezium'dan gelen tam Change Data Capture (CDC) mesajı.
/// </summary>
public class DebeziumMessage
{
    [JsonPropertyName("schema")]
    public DebeziumSchema? Schema { get; set; }
    
    [JsonPropertyName("payload")]
    public DebeziumPayload? Payload { get; set; }
}

/// <summary>
/// Debezium mesajının şema bilgisi.
/// </summary>
public class DebeziumSchema
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }
    
    [JsonPropertyName("fields")]
    public object[]? Fields { get; set; }
    
    [JsonPropertyName("optional")]
    public bool Optional { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("version")]
    public int Version { get; set; }
}

/// <summary>
/// Debezium mesajının veri içeriği.
/// </summary>
public class DebeziumPayload
{
    /// <summary>
    /// İşlemden önceki veri durumu. Silme (delete) işlemlerinde dolu, ekleme (create) işlemlerinde null.
    /// </summary>
    [JsonPropertyName("before")]
    public DebeziumTransferDto? Before { get; set; }
    
    /// <summary>
    /// İşlemden sonraki veri durumu. Ekleme (create) ve güncelleme (update) işlemlerinde dolu, silme (delete) işlemlerinde null.
    /// </summary>
    [JsonPropertyName("after")]
    public DebeziumTransferDto? After { get; set; }
    
    /// <summary>
    /// Değişiklik kaynağı hakkında bilgiler.
    /// </summary>
    [JsonPropertyName("source")]
    public DebeziumSource? Source { get; set; }
    
    /// <summary>
    /// İşlem türü: c=create, u=update, d=delete, r=read
    /// </summary>
    [JsonPropertyName("op")]
    public string? Op { get; set; }
    
    /// <summary>
    /// İşlem zamanı (milisaniye cinsinden).
    /// </summary>
    [JsonPropertyName("ts_ms")]
    public long TsMs { get; set; }
    
    /// <summary>
    /// İşlem zamanı (mikrosaniye cinsinden).
    /// </summary>
    [JsonPropertyName("ts_us")]
    public long TsUs { get; set; }
    
    /// <summary>
    /// İşlem zamanı (nanosaniye cinsinden).
    /// </summary>
    [JsonPropertyName("ts_ns")]
    public long TsNs { get; set; }
    
    /// <summary>
    /// İşlem bilgileri.
    /// </summary>
    [JsonPropertyName("transaction")]
    public object? Transaction { get; set; }
}

/// <summary>
/// Değişiklik kaynağı hakkında bilgiler.
/// </summary>
public class DebeziumSource
{
    [JsonPropertyName("version")]
    public string? Version { get; set; }
    
    [JsonPropertyName("connector")]
    public string? Connector { get; set; }
    
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    
    [JsonPropertyName("ts_ms")]
    public long TsMs { get; set; }
    
    [JsonPropertyName("snapshot")]
    public string? Snapshot { get; set; }
    
    [JsonPropertyName("db")]
    public string? Db { get; set; }
    
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }
    
    [JsonPropertyName("table")]
    public string? Table { get; set; }
    
    [JsonPropertyName("txId")]
    public long? TxId { get; set; }
    
    [JsonPropertyName("lsn")]
    public long? Lsn { get; set; }
    
    [JsonPropertyName("xmin")]
    public object? Xmin { get; set; }
} 