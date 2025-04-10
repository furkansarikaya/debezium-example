using System.Globalization;
using System.Text.Json.Serialization;

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
        decimal amount = 0;

        // Base64 olarak kodlanmış miktarı parse et
        if (!string.IsNullOrEmpty(AmountRaw))
        {
            try
            {
                // Base64 encoded bytes'ı decode et
                byte[] bytes = Convert.FromBase64String(AmountRaw);

                // Big-endian olarak integer değeri elde et (Debezium böyle yollar)
                Array.Reverse(bytes); // Eğer little-endian sistemde çalışıyorsan bunu yapabilirsin

                int unscaledValue = BitConverter.ToInt16(bytes, 0); // 500

                // Debezium schema'da scale: 2
                int scale = 2;

                amount = unscaledValue / (decimal)Math.Pow(10, scale);

                Console.WriteLine($"Generic decode: {amount}");
            }
            catch (Exception ex)
            {
                // Hata durumunda loglama
                Console.WriteLine($"Amount dönüşüm hatası: {ex.Message}");
                Console.WriteLine($"Amount raw value: {AmountRaw}");
            }
        }

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
            Amount = amount,
            CreatedAt = createdAtDate,
            Description = Description
        };
    }
}