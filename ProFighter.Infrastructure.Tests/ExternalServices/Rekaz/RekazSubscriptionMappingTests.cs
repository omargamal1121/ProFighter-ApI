using System;
using System.Collections.Generic;
using System.Text.Json;
using ProFighter.Infrastructure.ExternalServices.Rekaz;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;
using Xunit;

namespace ProFighter.Infrastructure.Tests.ExternalServices.Rekaz;

public class RekazSubscriptionMappingTests
{
    private static RekazSubscriptionDto CreateDto(List<RekazSubscriptionItemDto>? items)
    {
        return new RekazSubscriptionDto(
            Id: Guid.NewGuid(),
            SubscriptionCode: "SUB-12345",
            CustomerId: Guid.NewGuid(),
            StartAt: DateTime.UtcNow,
            EndAt: DateTime.UtcNow.AddMonths(1),
            Status: "Active",
            PaidAmount: 100m,
            TotalAmount: 100m,
            RemainingAmount: 0m,
            LastInvoiceStatus: "Paid",
            IsPaused: false,
            PausedAt: null,
            ResumeAt: null,
            BranchId: null,
            CreationTime: DateTime.UtcNow,
            LastModificationTime: null,
            Items: items ?? new List<RekazSubscriptionItemDto>(),
            Discount: null
        );
    }

    [Fact]
    public void MapSubscription_WithValidArabicName_PopulatesName()
    {
        // Arrange
        var localizedName = new RekazLocalizedNameDto(new Dictionary<string, string>
        {
            { "ar", "سباحة" },
            { "en", "Swimming" }
        });

        var items = new List<RekazSubscriptionItemDto>
        {
            new RekazSubscriptionItemDto(
                Id: Guid.NewGuid(),
                PriceId: Guid.NewGuid(),
                Name: "سباحة شهر",
                ProductName: "سباحة شهر",
                Quantity: 1,
                LocalizedProductName: localizedName)
        };

        var dto = CreateDto(items);

        // Act
        var result = RekazSubscriptionsClient.MapSubscription(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("سباحة", result.Name);
    }

    [Fact]
    public void MapSubscription_WithEmptyItemsArray_SetsNameNullWithoutThrowing()
    {
        // Arrange
        var dto = CreateDto(new List<RekazSubscriptionItemDto>());

        // Act
        var result = RekazSubscriptionsClient.MapSubscription(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public void MapSubscription_WithNullItemsList_SetsNameNullWithoutThrowing()
    {
        // Arrange
        var dto = CreateDto(null);

        // Act
        var result = RekazSubscriptionsClient.MapSubscription(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public void MapSubscription_WithMissingLocalizedProductName_SetsNameNullWithoutThrowing()
    {
        // Arrange
        var items = new List<RekazSubscriptionItemDto>
        {
            new RekazSubscriptionItemDto(
                Id: Guid.NewGuid(),
                PriceId: Guid.NewGuid(),
                Name: "سباحة شهر",
                ProductName: "سباحة شهر",
                Quantity: 1,
                LocalizedProductName: null)
        };

        var dto = CreateDto(items);

        // Act
        var result = RekazSubscriptionsClient.MapSubscription(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public void MapSubscription_WithMissingArKey_SetsNameNullWithoutThrowing()
    {
        // Arrange
        var localizedName = new RekazLocalizedNameDto(new Dictionary<string, string>
        {
            { "en", "Swimming" }
        });

        var items = new List<RekazSubscriptionItemDto>
        {
            new RekazSubscriptionItemDto(
                Id: Guid.NewGuid(),
                PriceId: Guid.NewGuid(),
                Name: "Swimming 1 Month",
                ProductName: "Swimming",
                Quantity: 1,
                LocalizedProductName: localizedName)
        };

        var dto = CreateDto(items);

        // Act
        var result = RekazSubscriptionsClient.MapSubscription(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Name);
    }

    [Fact]
    public void DeserializeAndMap_FromRealRekazJson_ExtractsArabicName()
    {
        // Arrange
        var json = """
        {
            "id": "e838f375-bcf7-4f66-8a71-61b6bbdb6e99",
            "subscriptionCode": "SUB-999",
            "customerId": "d838f375-bcf7-4f66-8a71-61b6bbdb6e99",
            "startAt": "2026-09-01T00:00:00Z",
            "endAt": "2026-10-01T00:00:00Z",
            "status": "Active",
            "paidAmount": 250.0,
            "totalAmount": 250.0,
            "remainingAmount": 0.0,
            "lastInvoiceStatus": "Paid",
            "isPaused": false,
            "items": [
                {
                    "id": "11111111-2222-3333-4444-555555555555",
                    "priceId": "66666666-7777-8888-9999-000000000000",
                    "name": "ملاكمة 3 شهور",
                    "productName": "ملاكمة",
                    "quantity": 1,
                    "localizedProductName": {
                        "otherLanguages": {
                            "ar": "ملاكمة",
                            "en": "Boxing"
                        }
                    }
                }
            ]
        }
        """;

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new RekazDiscountTypeConverter() }
        };

        // Act
        var dto = JsonSerializer.Deserialize<RekazSubscriptionDto>(json, jsonOptions);
        Assert.NotNull(dto);

        var result = RekazSubscriptionsClient.MapSubscription(dto!);

        // Assert
        Assert.Equal("ملاكمة", result.Name);
    }
}
