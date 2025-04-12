using System.Globalization;
using System.Text.Json.Serialization;
using Transfer.Shared.Parser;

namespace Transfer.Shared.Models;

/// <summary>
/// Debezium'dan gelen transfer verilerini temsil eden DTO (Data Transfer Object)
/// </summary>
public class DebeziumTransferDto
{
    [JsonPropertyName("Id")] public int Id { get; set; }

    [JsonPropertyName("FromAccount")] public int FromAccount { get; set; }

    [JsonPropertyName("ToAccount")] public int ToAccount { get; set; }

    [JsonPropertyName("Amount")] public string? AmountRaw { get; set; }

    [JsonPropertyName("CreatedAt")] public string? CreatedAt { get; set; }

    [JsonPropertyName("Description")] public string? Description { get; set; }

    /// <summary>
    /// Bu DTO'yu bir TransferEntity'ye dönüştürür
    /// </summary>
    public TransferEntity ToEntity()
    {
        DateTime createdAtDate = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(CreatedAt))
        {
            // Debezium'un standart zaman formatı: ISO 8601
            if (DateTime.TryParse(CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out DateTime parsedDate))
            {
                createdAtDate = parsedDate;
            }
        }

        return new TransferEntity
        {
            Id = Id,
            FromAccount = FromAccount,
            ToAccount = ToAccount,
            CreatedAt = createdAtDate,
            Description = Description
        };
    }
}