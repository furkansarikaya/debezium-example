using System.Text.Json.Nodes;

namespace Transfer.Shared.Parser;

public class DebeziumDecimalParser
{
    public static decimal ParseAmount(JsonNode root)
    {
        // 1. "Amount" değeri
        string base64Amount = root["payload"]?["after"]?["Amount"]?.ToString();
        if (string.IsNullOrEmpty(base64Amount))
            throw new Exception("Amount not found");

        // 2. "scale" değerini schema'dan bul
        var fields = root["schema"]?["fields"];
        int? scale = null;
        foreach (var field in fields!.AsArray())
        {
            if (field?["field"]?.ToString() == "after")
            {
                var afterFields = field["fields"];
                foreach (var afterField in afterFields!.AsArray())
                {
                    if (afterField?["field"]?.ToString() == "Amount")
                    {
                        string? scaleStr = afterField?["parameters"]?["scale"]?.ToString();
                        scale = int.Parse(scaleStr!);
                        break;
                    }
                }
            }
        }

        if (scale == null)
            throw new Exception("Scale not found");

        // 3. Decode + scale uygula
        byte[] bytes = Convert.FromBase64String(base64Amount);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);

        int unscaledValue = BitConverter.ToInt16(bytes, 0); // 2 byte
        decimal value = unscaledValue / (decimal)Math.Pow(10, scale.Value);
        return value;
    }
}