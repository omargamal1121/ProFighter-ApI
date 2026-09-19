using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ProFighter.Application.Common.Exceptions;
using ProFighter.Infrastructure.ExternalServices.Rekaz;
using ProFighter.Infrastructure.ExternalServices.Rekaz.Dtos;
using Xunit;

namespace ProFighter.Infrastructure.Tests.ExternalServices.Rekaz;

public class RekazSubscriptionsClientFilterTests
{
    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_response);
        }
    }

    [Fact]
    public async Task GetSubscriptionsByCustomerAsync_WhenReturnedItemsHaveMismatchedCustomerId_ThrowsRekazCustomerFilterNotSupportedException()
    {
        // Arrange
        var requestedCustomerId = Guid.NewGuid();
        var mismatchedCustomerId = Guid.NewGuid();

        var responseJson = $$"""
        {
            "items": [
                {
                    "id": "{{Guid.NewGuid()}}",
                    "subscriptionCode": "SUB-123",
                    "customerId": "{{mismatchedCustomerId}}",
                    "startAt": "2026-09-01T00:00:00Z",
                    "endAt": "2026-10-01T00:00:00Z",
                    "status": "Active",
                    "paidAmount": 100.0,
                    "totalAmount": 100.0,
                    "remainingAmount": 0.0,
                    "lastInvoiceStatus": "Paid",
                    "isPaused": false,
                    "items": []
                }
            ],
            "totalCount": 1
        }
        """;

        var httpResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(responseJson, System.Text.Encoding.UTF8, "application/json")
        };

        var handler = new FakeHttpMessageHandler(httpResponse);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.rekaz.test") };
        var logger = Substitute.For<ILogger<RekazSubscriptionsClient>>();

        var client = new RekazSubscriptionsClient(httpClient, logger);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<RekazCustomerFilterNotSupportedException>(() =>
            client.GetSubscriptionsByCustomerAsync(requestedCustomerId));

        Assert.Equal(requestedCustomerId, exception.RequestedCustomerId);
        Assert.Equal(mismatchedCustomerId, exception.MismatchedCustomerId);
    }
}
