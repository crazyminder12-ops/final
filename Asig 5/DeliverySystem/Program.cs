namespace DeliverySystem;

public abstract record Result<T, E>
{
    public sealed record Success(T Value) : Result<T, E>;
    public sealed record Failure(E Error) : Result<T, E>;
    public Result<U, E> Map<U>(Func<T, U> transform) => this switch
    {
        Success(var value) => new Result<U, E>.Success(transform(value)),
        Failure(var error) => new Result<U, E>.Failure(error),
        _ => throw new InvalidOperationException("Unknown Result case.")
    };
    public Result<U, E> Bind<U>(Func<T, Result<U, E>> next) => this switch
    {
        Success(var value) => next(value),
        Failure(var error) => new Result<U, E>.Failure(error),
        _ => throw new InvalidOperationException("Unknown Result case.")
    };
}

public sealed record DomainError(string Code, string Message);
public sealed record DeliveryInput(string CustomerName, double DistanceKm);
public sealed record DeliveryRequest(string CustomerName, double DistanceKm);
public sealed record DeliveryQuote(string CustomerName, double DistanceKm, decimal Price);
public sealed record QuoteSummary(string Text);
public abstract record DeliveryState;
public sealed record PendingDelivery(DeliveryRequest Request) : DeliveryState;
public sealed record QuotedDelivery(DeliveryRequest Request, DeliveryQuote Quote) : DeliveryState;
public sealed record FailedDelivery(string Reason) : DeliveryState;

public static class DeliveryService
{
    public static Result<DeliveryRequest, DomainError> CreateRequest(DeliveryInput input)
    {
        if (string.IsNullOrWhiteSpace(input.CustomerName))
            return new Result<DeliveryRequest, DomainError>.Failure(new("INVALID_CUSTOMER", "Customer name cannot be empty."));
        if (input.DistanceKm < 0 || input.DistanceKm > 50)
            return new Result<DeliveryRequest, DomainError>.Failure(new("INVALID_DISTANCE", "Distance must be between 0 and 50 km."));
        return new Result<DeliveryRequest, DomainError>.Success(new(input.CustomerName.Trim(), input.DistanceKm));
    }
    public static Result<DeliveryQuote, DomainError> CreateQuote(DeliveryRequest request)
    {
        if (request.DistanceKm < 0 || request.DistanceKm > 50)
            return new Result<DeliveryQuote, DomainError>.Failure(new("INVALID_DISTANCE", "Cannot quote a delivery outside 0-50 km."));
        return new Result<DeliveryQuote, DomainError>.Success(new(request.CustomerName, request.DistanceKm, 500m + 100m * (decimal)request.DistanceKm));
    }
    public static Result<QuoteSummary, DomainError> BuildQuote(DeliveryRequest request) =>
        CreateQuote(request).Map(q => new QuoteSummary($"{q.CustomerName} | {q.DistanceKm} km | {q.Price} KZT"));
    public static decimal StandardPrice(DeliveryRequest request) => 500m + 100m * (decimal)request.DistanceKm;
    public static decimal ExpressPrice(DeliveryRequest request) => 800m + 150m * (decimal)request.DistanceKm;
    public static List<DeliveryQuote> RunPipeline(IEnumerable<DeliveryRequest> requests, Func<DeliveryRequest, decimal> pricePolicy) =>
        requests.Select(r => new DeliveryQuote(r.CustomerName, r.DistanceKm, pricePolicy(r))).OrderBy(q => q.Price).ToList();
    public static DeliveryState CalculateQuote(DeliveryState state, Func<DeliveryRequest, decimal> pricePolicy) => state switch
    {
        PendingDelivery p => new QuotedDelivery(p.Request, new DeliveryQuote(p.Request.CustomerName, p.Request.DistanceKm, pricePolicy(p.Request))),
        QuotedDelivery => new FailedDelivery("This delivery has already been quoted."),
        FailedDelivery f => f,
        _ => new FailedDelivery("Unknown delivery state.")
    };
}

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("DELIVERY SYSTEM — ASSIGNMENT 7 (LIMAN BASE)");
        var request = DeliveryService.CreateRequest(new("Aruzhan", 2));
        var result = request.Bind(DeliveryService.CreateQuote).Map(q => new QuoteSummary($"{q.CustomerName} | {q.Price} KZT"));
        switch (result)
        {
            case Result<QuoteSummary, DomainError>.Success s: Console.WriteLine(s.Value.Text); break;
            case Result<QuoteSummary, DomainError>.Failure f: Console.WriteLine($"[{f.Error.Code}] {f.Error.Message}"); break;
        }
    }
}
