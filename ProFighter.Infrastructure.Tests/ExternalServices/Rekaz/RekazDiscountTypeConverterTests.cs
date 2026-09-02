using System.Text.Json;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;
using Xunit;

namespace ProFighter.Infrastructure.Tests.ExternalServices.Rekaz;

public class RekazDiscountTypeConverterTests
{
    private readonly JsonSerializerOptions _options;
    private readonly RekazDiscountTypeConverter _converter;

    public RekazDiscountTypeConverterTests()
    {
        _options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new RekazDiscountTypeConverter() }
        };
        _converter = new RekazDiscountTypeConverter();
    }

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("42")]
    public void Read_WhenTypeIsNumericString_ReturnsStringValue(string jsonValue)
    {
        // Arrange
        var json = $"{{\"type\": \"{jsonValue}\", \"value\": 10.5}}";

        // Act
        var result = JsonSerializer.Deserialize<RekazDiscountDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jsonValue, result.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(42)]
    public void Read_WhenTypeIsNumber_ReturnsStringValue(int jsonValue)
    {
        // Arrange
        var json = $"{{\"type\": {jsonValue}, \"value\": 10.5}}";

        // Act
        var result = JsonSerializer.Deserialize<RekazDiscountDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jsonValue.ToString(), result.Type);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10.5)]
    [InlineData(99.99)]
    public void Read_WhenValueIsNumber_ReturnsDecimalValue(decimal jsonValue)
    {
        // Arrange
        var json = $"{{\"type\": \"Fixed\", \"value\": {jsonValue}}}";

        // Act
        var result = JsonSerializer.Deserialize<RekazDiscountDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(jsonValue, result.Value);
    }

    [Fact]
    public void Read_WhenDiscountIsNull_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var result = JsonSerializer.Deserialize<RekazDiscountDto?>(json, _options);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Read_WhenTypeIsStringEnumLikeFixed_ReturnsString()
    {
        // Arrange
        var json = "{\"type\": \"Fixed\", \"value\": 0}";

        // Act
        var result = JsonSerializer.Deserialize<RekazDiscountDto>(json, _options);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Fixed", result.Type);
    }

    [Fact]
    public void Write_WhenWritingString_WritesString()
    {
        // Arrange
        var discount = new RekazDiscountDto("Fixed", 10.5m);

        // Act
        var json = JsonSerializer.Serialize(discount, _options);

        // Assert
        Assert.Contains("\"type\":\"Fixed\"", json);
    }

    [Fact]
    public void Read_WhenTypeIsUnexpectedTokenType_ThrowsJsonException()
    {
        // Arrange
        var json = "{\"type\": true, \"value\": 10.5}";
        var reader = new Utf8JsonReader(System.Text.Encoding.UTF8.GetBytes(json));
        reader.Read(); // Skip start object
        reader.Read(); // Skip property name
        reader.Read(); // Get to the value

        // Act & Assert
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<RekazDiscountDto>(json, _options));
    }
}
