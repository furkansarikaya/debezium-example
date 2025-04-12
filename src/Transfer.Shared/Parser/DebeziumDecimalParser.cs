using System.Numerics;
using System.Text.Json.Nodes;

namespace Transfer.Shared.Parser;

public static class DebeziumDecimalParser
{
    public static decimal ParseAmount(JsonNode root)
    {
        try
        {
            // 1. "Amount" değeri
            string? base64Amount = root["payload"]?["after"]?["Amount"]?.ToString();
            if (string.IsNullOrEmpty(base64Amount))
                throw new Exception("Amount not found in after section");

            // 2. "scale" değerini schema'dan bul
            var fields = root["schema"]?["fields"];
            if (fields == null)
                throw new Exception("Schema fields not found");

            int? scale = FindScaleFromSchema(fields);
            if (scale == null)
                throw new Exception("Scale not found in schema");

            // 3. Base64 decode + BigInteger + scale uygula
            byte[] bytes = Convert.FromBase64String(base64Amount);
            Array.Reverse(bytes); // big-endian → little-endian

            BigInteger unscaled = new BigInteger(bytes);
            decimal value = (decimal)unscaled / (decimal)Math.Pow(10, scale.Value);
            return value;
        }
        catch (FormatException)
        {
            throw new Exception($"Invalid base64 format for Amount: {root["payload"]?["after"]?["Amount"]}");
        }
        catch (Exception ex) when (
            !ex.Message.StartsWith("Amount not found") &&
            !ex.Message.StartsWith("Scale not found") &&
            !ex.Message.StartsWith("Schema fields not found") &&
            !ex.Message.StartsWith("Invalid base64 format"))
        {
            throw new Exception($"Error parsing amount: {ex.Message}", ex);
        }
    }

    private static int? FindScaleFromSchema(JsonNode? fields)
    {
        foreach (var field in fields!.AsArray())
        {
            if (field?["field"]?.ToString() == "after")
            {
                var afterFields = field["fields"];
                foreach (var afterField in afterFields!.AsArray())
                {
                    if (afterField?["field"]?.ToString() == "Amount")
                    {
                        string? scaleStr = afterField["parameters"]?["scale"]?.ToString();
                        if (int.TryParse(scaleStr, out var scale))
                            return scale;
                    }
                }
            }
        }

        return null;
    }
}