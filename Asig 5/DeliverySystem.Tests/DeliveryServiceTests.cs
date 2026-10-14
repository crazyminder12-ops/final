using System;
using System.Collections.Generic;
using DeliverySystem;
using Xunit;

public class DeliveryServiceTests
{
    [Fact]
    public void NormalWorkflow_CalculatesAndSortsQuotes()
    {
        var requests = new List<DeliveryRequest>
        {
            new DeliveryRequest("Far", 5),
            new DeliveryRequest("Near", 1)
        };

        var result = DeliveryService.RunPipeline(requests, DeliveryService.StandardPrice);

        Assert.Equal(2, result.Count);
        Assert.Equal("Near", result[0].CustomerName);
        Assert.Equal(600m, result[0].Price);
        Assert.Equal(1000m, result[1].Price);
    }

    [Fact]
    public void EmptyDataset_ReturnsNoQuotes()
    {
        var result = DeliveryService.RunPipeline(
            new List<DeliveryRequest>(), DeliveryService.StandardPrice);

        Assert.Empty(result);
    }

    [Fact]
    public void BoundaryValue_ZeroDistanceCostsBasePrice()
    {
        var request = new DeliveryRequest("Nearby", 0);

        var result = DeliveryService.CalculatePrices(
            new List<DeliveryRequest> { request }, DeliveryService.StandardPrice);

        Assert.Equal(500m, result[0].Price);
    }

    [Fact]
    public void NegativeDistance_IsRejected()
    {
        Assert.Throws<ArgumentException>(
            () => new DeliveryRequest("Invalid", -1));
    }

    [Fact]
    public void TwoPolicies_ReturnDifferentPrices()
    {
        var request = new DeliveryRequest("Customer", 2);

        Assert.Equal(700m, DeliveryService.StandardPrice(request));
        Assert.Equal(1100m, DeliveryService.ExpressPrice(request));
    }

    [Fact]
    public void SelectingRequests_DoesNotChangeOriginalList()
    {
        var requests = new List<DeliveryRequest>
        {
            new DeliveryRequest("One", 1),
            new DeliveryRequest("Two", 2)
        };

        var result = DeliveryService.SelectValidRequests(requests);

        Assert.Equal(2, requests.Count);
        Assert.Equal(2, result.Count);
    }


    [Fact]
    public void RequestOver50Km_IsNotSelected()
    {
        var requests = new List<DeliveryRequest>
        {
            new DeliveryRequest("Nearby", 10),
            new DeliveryRequest("Too far", 60)
        };

        var result = DeliveryService.SelectValidRequests(requests);

        Assert.Single(result);
        Assert.Equal("Nearby", result[0].CustomerName);
    }

    [Fact]
    public void MissingList_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(
            () => DeliveryService.SelectValidRequests(null!));
    }
}
