using DeliverySystem;
using Xunit;

public class DeliveryServiceTests
{
    [Fact]
    public void CreateRequest_ReturnsSuccessForValidInput()
    {
        var result = DeliveryService.CreateRequest(new("Aruzhan", 2));
        var success = Assert.IsType<Result<DeliveryRequest, DomainError>.Success>(result);
        Assert.Equal("Aruzhan", success.Value.CustomerName);
    }

    [Fact]
    public void CreateRequest_RejectsEmptyCustomer()
    {
        var result = Assert.IsType<Result<DeliveryRequest, DomainError>.Failure>(
            DeliveryService.CreateRequest(new(" ", 2)));
        Assert.Equal("INVALID_CUSTOMER", result.Error.Code);
    }

    [Fact]
    public void CreateRequest_RejectsNegativeDistance()
    {
        var result = Assert.IsType<Result<DeliveryRequest, DomainError>.Failure>(
            DeliveryService.CreateRequest(new("Aruzhan", -1)));
        Assert.Equal("INVALID_DISTANCE", result.Error.Code);
    }

    [Fact]
    public void CreateRequest_RejectsDistanceAboveLimit()
    {
        var result = Assert.IsType<Result<DeliveryRequest, DomainError>.Failure>(
            DeliveryService.CreateRequest(new("Aruzhan", 51)));
        Assert.Equal("INVALID_DISTANCE", result.Error.Code);
    }

    [Fact]
    public void Lookup_ReturnsSomeWhenCustomerExists()
    {
        var request = new DeliveryRequest("Aruzhan", 2);
        Assert.Equal(new Option<DeliveryRequest>.Some(request),
            DeliveryService.FindRequest(new[] { request }, "Aruzhan"));
    }

    [Fact]
    public void Lookup_ReturnsNoneWhenCustomerIsMissing()
    {
        Assert.IsType<Option<DeliveryRequest>.None>(
            DeliveryService.FindRequest(Array.Empty<DeliveryRequest>(), "Nobody"));
    }

    [Fact]
    public void ZeroDistanceIsAValidValueNotAbsence()
    {
        var request = new DeliveryRequest("Ali", 0);
        var lookup = DeliveryService.FindRequest(new[] { request }, "Ali");
        var success = Assert.IsType<Result<DeliveryRequest, DomainError>.Success>(
            DeliveryService.RequireRequest(lookup));
        Assert.Equal(0, success.Value.DistanceKm);
    }

    [Fact]
    public void RequireRequestConvertsNoneToUsefulFailure()
    {
        var result = Assert.IsType<Result<DeliveryRequest, DomainError>.Failure>(
            DeliveryService.RequireRequest(new Option<DeliveryRequest>.None()));
        Assert.Equal("DELIVERY_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public void MapTransformsSuccessfulValue()
    {
        var source = new Result<int, string>.Success(5);
        var mapped = source.Map(x => x * 2);
        Assert.Equal(10, Assert.IsType<Result<int, string>.Success>(mapped).Value);
    }

    [Fact]
    public void BindContinuesWithSuccessfulValue()
    {
        var source = new Result<int, string>.Success(5);
        var bound = source.Bind(x => new Result<string, string>.Success($"value={x}"));
        Assert.Equal("value=5", Assert.IsType<Result<string, string>.Success>(bound).Value);
    }

    [Fact]
    public void MapPropagatesFailureWithoutCallingFunction()
    {
        var called = false;
        var source = new Result<int, string>.Failure("bad");
        var mapped = source.Map(x => { called = true; return x + 1; });
        Assert.False(called);
        Assert.Equal("bad", Assert.IsType<Result<int, string>.Failure>(mapped).Error);
    }

    [Fact]
    public void BindPropagatesFailureWithoutCallingFunction()
    {
        var called = false;
        var source = new Result<int, string>.Failure("bad");
        var bound = source.Bind(x => { called = true; return new Result<int, string>.Success(x); });
        Assert.False(called);
        Assert.Equal("bad", Assert.IsType<Result<int, string>.Failure>(bound).Error);
    }

    [Fact]
    public void BuildQuoteRunsIntegratedBindAndMapChain()
    {
        var request = new DeliveryRequest("Aruzhan", 2);
        var result = DeliveryService.BuildQuote(new[] { request }, "Aruzhan");
        var success = Assert.IsType<Result<QuoteSummary, DomainError>.Success>(result);
        Assert.Equal("Aruzhan | 2 km | 700 KZT", success.Value.Text);
    }

    [Fact]
    public void BuildQuoteReturnsFailureForMissingCustomer()
    {
        var result = Assert.IsType<Result<QuoteSummary, DomainError>.Failure>(
            DeliveryService.BuildQuote(Array.Empty<DeliveryRequest>(), "Nobody"));
        Assert.Equal("DELIVERY_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public void LoadFromJsonReportsMissingFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        var result = Assert.IsType<Result<List<DeliveryInput>, DomainError>.Failure>(
            DeliveryService.LoadFromJson(path));
        Assert.Equal("FILE_NOT_FOUND", result.Error.Code);
    }

    [Fact]
    public void LoadFromJsonReportsMalformedContent()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, "{ invalid json");
            var result = Assert.IsType<Result<List<DeliveryInput>, DomainError>.Failure>(
                DeliveryService.LoadFromJson(path));
            Assert.Equal("MALFORMED_JSON", result.Error.Code);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void LoadFromJsonReadsValidFile()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".json");
        try
        {
            File.WriteAllText(path, """[{"CustomerName":"Ali","DistanceKm":0}]""");
            var result = Assert.IsType<Result<List<DeliveryInput>, DomainError>.Success>(
                DeliveryService.LoadFromJson(path));
            Assert.Single(result.Value);
            Assert.Equal(0, result.Value[0].DistanceKm);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void Assignment6PipelineStillSortsStandardQuotes()
    {
        var requests = new[] { new DeliveryRequest("Far", 5), new DeliveryRequest("Near", 1) };
        var quotes = DeliveryService.RunPipeline(requests, DeliveryService.StandardPrice);
        Assert.Equal("Near", quotes[0].CustomerName);
        Assert.Equal(600m, quotes[0].Price);
    }

    [Fact]
    public void SafeStateTransitionRejectsRepeatedQuote()
    {
        var request = new DeliveryRequest("Customer", 2);
        DeliveryState quoted = DeliveryService.CalculateQuote(
            new PendingDelivery(request), DeliveryService.StandardPrice);
        var repeated = Assert.IsType<FailedDelivery>(
            DeliveryService.CalculateQuote(quoted, DeliveryService.StandardPrice));
        Assert.Contains("already", repeated.Reason);
    }
}
