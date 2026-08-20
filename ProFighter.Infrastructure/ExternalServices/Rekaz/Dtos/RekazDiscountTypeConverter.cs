using System.Text.Json;
using System.Text.Json.Serialization;

namespace ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;

/// <summary>
/// Custom JSON converter for discount type that handles both numeric (0) and string ("0") representations.
/// Rekaz API may return discount.type as either format, so this converter normalizes both to string.
/// </summary>
public sealed class RekazDiscountTypeConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle numeric representation (e.g., 0)
            return reader.GetInt32().ToString();
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            // Handle string representation (e.g., "0")
            return reader.GetString() ?? throw new JsonException("Discount type string value is null.");
        }
        else
        {
            throw new JsonException($"Unexpected token type for discount type: {reader.TokenType}");
        }
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
