using System;
using System.Collections.Generic;
using DeliverySystem;
using Xunit;

public class DeliveryServiceTests
{
    [Fact]
    public void FactorySuccess_CreatesValidatedRequest()
    {
        var result = DeliveryRequest.Create(" Customer ", 2);
        var valid = Assert.IsType<ValidDeliveryRequest>(result);
        Assert.Equal("Customer", valid.Value.CustomerName);
        Assert.Equal(2, valid.Value.DistanceKm);
    }

    [Fact]
    public void FactoryRejectsEmptyName()
    {
        var result = Assert.IsType<InvalidDeliveryRequest>(DeliveryRequest.Create(" ", 2));
        Assert.Contains(result.Errors, error => error.Contains("name"));
    }

    [Fact]
    public void FactoryRejectsNegativeDistance()
    {
        var result = Assert.IsType<InvalidDeliveryRequest>(DeliveryRequest.Create("Customer", -1));
        Assert.Contains(result.Errors, error => error.Contains("Distance"));
    }

    [Fact]
    public void FactoryRejectsDistanceAboveMaximum()
    {
        Assert.IsType<InvalidDeliveryRequest>(DeliveryRequest.Create("Customer", 51));
    }

    [Fact]
    public void BoundaryValue_ZeroDistanceCostsBasePrice()
    {
        var valid = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Nearby", 0));
        Assert.Equal(500m, DeliveryService.StandardPrice(valid.Value));
    }

    [Fact]
    public void BoundaryValue_FiftyKmIsAccepted()
    {
        var valid = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Edge", 50));
        Assert.Equal(5500m, DeliveryService.StandardPrice(valid.Value));
    }

    [Fact]
    public void NormalWorkflow_CalculatesAndSortsQuotes()
    {
        var near = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Near", 1)).Value;
        var far = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Far", 5)).Value;
        var result = DeliveryService.RunPipeline(new List<DeliveryRequest> { far, near }, DeliveryService.StandardPrice);

        Assert.Equal(2, result.Count);
        Assert.Equal("Near", result[0].CustomerName);
        Assert.Equal(600m, result[0].Price);
        Assert.Equal(1000m, result[1].Price);
    }

    [Fact]
    public void EmptyDataset_ReturnsNoQuotes()
    {
        Assert.Empty(DeliveryService.RunPipeline(new List<DeliveryRequest>(), DeliveryService.StandardPrice));
    }

    [Fact]
    public void TwoPolicies_ReturnDifferentPrices()
    {
        var request = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Customer", 2)).Value;
        Assert.Equal(700m, DeliveryService.StandardPrice(request));
        Assert.Equal(1100m, DeliveryService.ExpressPrice(request));
    }

    [Fact]
    public void PatternMatching_HandlesBothFactoryCases()
    {
        DeliveryRequestResult valid = DeliveryRequest.Create("Customer", 1);
        DeliveryRequestResult invalid = DeliveryRequest.Create("", 1);

        Assert.Equal("valid", Describe(valid));
        Assert.Equal("invalid", Describe(invalid));
    }

    [Fact]
    public void ValidPendingDelivery_TransitionsToQuoted()
    {
        var request = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Customer", 2)).Value;
        DeliveryState result = DeliveryService.CalculateQuote(
            new PendingDelivery(request), DeliveryService.StandardPrice);

        var quoted = Assert.IsType<QuotedDelivery>(result);
        Assert.Equal(700m, quoted.Quote.Price);
    }

    [Fact]
    public void MissingPrerequisite_InvalidFactoryResultCannotBecomePending()
    {
        var result = DeliveryRequest.Create("", 2);
        Assert.IsType<InvalidDeliveryRequest>(result);
        // The invalid case contains errors, not a usable DeliveryRequest payload.
        Assert.NotEmpty(Assert.IsType<InvalidDeliveryRequest>(result).Errors);
    }

    [Fact]
    public void RepeatedQuote_IsRejectedAndOriginalStateIsPreserved()
    {
        var request = Assert.IsType<ValidDeliveryRequest>(DeliveryRequest.Create("Customer", 2)).Value;
        DeliveryState original = new PendingDelivery(request);
        DeliveryState quoted = DeliveryService.CalculateQuote(original, DeliveryService.StandardPrice);
        DeliveryState repeated = DeliveryService.CalculateQuote(quoted, DeliveryService.StandardPrice);

        Assert.IsType<PendingDelivery>(original);
        Assert.IsType<QuotedDelivery>(quoted);
        Assert.Contains("already", Assert.IsType<FailedDelivery>(repeated).Reason);
    }

    [Fact]
    public void NullInputList_IsRejected()
    {
        Assert.Throws<ArgumentNullException>(() =>
            DeliveryService.SelectValidRequests(null!));
    }

    private static string Describe(DeliveryRequestResult result) => result switch
    {
        ValidDeliveryRequest => "valid",
        InvalidDeliveryRequest => "invalid",
        _ => "unknown"
    };
}
