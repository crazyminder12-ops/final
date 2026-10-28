using System.Text.Json;

namespace DeliverySystem;

// Explicit success/failure model. A failure never contains a success value.
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

public abstract record Option<T>
{
    public sealed record Some(T Value) : Option<T>;
    public sealed record None : Option<T>;
}

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
            return new Result<DeliveryRequest, DomainError>.Failure(
                new("INVALID_CUSTOMER", "Customer name cannot be empty."));
        if (input.DistanceKm < 0 || input.DistanceKm > 50)
            return new Result<DeliveryRequest, DomainError>.Failure(
                new("INVALID_DISTANCE", "Distance must be between 0 and 50 km."));
        return new Result<DeliveryRequest, DomainError>.Success(
            new(input.CustomerName.Trim(), input.DistanceKm));
    }

    public static Option<DeliveryRequest> FindRequest(
        IEnumerable<DeliveryRequest> requests, string customerName)
    {
        var found = requests.FirstOrDefault(r =>
            string.Equals(r.CustomerName, customerName, StringComparison.OrdinalIgnoreCase));
        return found is null
            ? new Option<DeliveryRequest>.None()
            : new Option<DeliveryRequest>.Some(found);
    }

    public static Result<DeliveryRequest, DomainError> RequireRequest(
        Option<DeliveryRequest> lookup) => lookup switch
    {
        Option<DeliveryRequest>.Some(var request) =>
            new Result<DeliveryRequest, DomainError>.Success(request),
        Option<DeliveryRequest>.None =>
            new Result<DeliveryRequest, DomainError>.Failure(
                new("DELIVERY_NOT_FOUND", "No delivery was found for the requested customer.")),
        _ => new Result<DeliveryRequest, DomainError>.Failure(
            new("UNKNOWN_LOOKUP", "Unknown lookup outcome."))
    };

    public static Result<DeliveryQuote, DomainError> CreateQuote(DeliveryRequest request)
    {
        if (request.DistanceKm < 0 || request.DistanceKm > 50)
            return new Result<DeliveryQuote, DomainError>.Failure(
                new("INVALID_DISTANCE", "Cannot quote a delivery outside 0-50 km."));
        var price = 500m + 100m * (decimal)request.DistanceKm;
        return new Result<DeliveryQuote, DomainError>.Success(
            new(request.CustomerName, request.DistanceKm, price));
    }

    public static Result<QuoteSummary, DomainError> BuildQuote(
        IEnumerable<DeliveryRequest> requests, string customerName)
    {
        // Bind continues with an operation that can fail; Map transforms a successful quote.
        return RequireRequest(FindRequest(requests, customerName))
            .Bind(CreateQuote)
            .Map(q => new QuoteSummary(
                $"{q.CustomerName} | {q.DistanceKm} km | {q.Price} KZT"));
    }

    // Assignment 6 pipeline retained and extended for Assignment 7.
    public static decimal StandardPrice(DeliveryRequest request) =>
        500m + 100m * (decimal)request.DistanceKm;

    public static decimal ExpressPrice(DeliveryRequest request) =>
        800m + 150m * (decimal)request.DistanceKm;

    public static List<DeliveryQuote> RunPipeline(
        IEnumerable<DeliveryRequest> requests, Func<DeliveryRequest, decimal> pricePolicy) =>
        requests.Select(r => new DeliveryQuote(r.CustomerName, r.DistanceKm, pricePolicy(r)))
            .OrderBy(q => q.Price).ToList();

    // The state transition prevents a second quote from being created.
    public static DeliveryState CalculateQuote(
        DeliveryState state, Func<DeliveryRequest, decimal> pricePolicy) => state switch
    {
        PendingDelivery pending => new QuotedDelivery(
            pending.Request,
            new DeliveryQuote(pending.Request.CustomerName, pending.Request.DistanceKm,
                pricePolicy(pending.Request))),
        QuotedDelivery => new FailedDelivery("This delivery has already been quoted."),
        FailedDelivery failed => failed,
        _ => new FailedDelivery("Unknown delivery state.")
    };

    public static Result<List<DeliveryInput>, DomainError> LoadFromJson(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var data = JsonSerializer.Deserialize<List<DeliveryInput>>(json);
            if (data is null)
                return new Result<List<DeliveryInput>, DomainError>.Failure(
                    new("EMPTY_JSON", $"JSON file '{path}' did not contain a delivery list."));
            return new Result<List<DeliveryInput>, DomainError>.Success(data);
        }
        catch (FileNotFoundException)
        {
            return new Result<List<DeliveryInput>, DomainError>.Failure(
                new("FILE_NOT_FOUND", $"Input file '{path}' was not found."));
        }
        catch (DirectoryNotFoundException)
        {
            return new Result<List<DeliveryInput>, DomainError>.Failure(
                new("DIRECTORY_NOT_FOUND", $"Directory for input file '{path}' was not found."));
        }
        catch (JsonException ex)
        {
            return new Result<List<DeliveryInput>, DomainError>.Failure(
                new("MALFORMED_JSON", $"Input file '{path}' contains malformed JSON: {ex.Message}"));
        }
        catch (IOException ex)
        {
            return new Result<List<DeliveryInput>, DomainError>.Failure(
                new("FILE_READ_ERROR", $"Could not read '{path}': {ex.Message}"));
        }
    }
}

public static class Program
{
    public static void Main()
    {
        Console.WriteLine("DELIVERY SYSTEM — ASSIGNMENT 7");
        var loaded = DeliveryService.LoadFromJson("deliveries.json");
        if (loaded is Result<List<DeliveryInput>, DomainError>.Failure loadError)
        {
            PrintError(loadError.Error);
            return;
        }

        var inputs = ((Result<List<DeliveryInput>, DomainError>.Success)loaded).Value;
        var requests = new List<DeliveryRequest>();
        foreach (var input in inputs)
        {
            var created = DeliveryService.CreateRequest(input);
            if (created is Result<DeliveryRequest, DomainError>.Success valid)
                requests.Add(valid.Value);
            else if (created is Result<DeliveryRequest, DomainError>.Failure invalid)
                PrintError(invalid.Error);
        }

        Console.WriteLine("\nSUCCESS:");
        PrintOutcome(DeliveryService.BuildQuote(requests, "Aruzhan"));

        Console.WriteLine("\nDOMAIN FAILURE (missing customer):");
        PrintOutcome(DeliveryService.BuildQuote(requests, "Unknown"));

        Console.WriteLine("\nFILE FAILURE (missing file):");
        var missing = DeliveryService.LoadFromJson("missing-deliveries.json");
        if (missing is Result<List<DeliveryInput>, DomainError>.Failure missingError)
            PrintError(missingError.Error);
    }

    private static void PrintOutcome(Result<QuoteSummary, DomainError> result)
    {
        switch (result)
        {
            case Result<QuoteSummary, DomainError>.Success success:
                Console.WriteLine(success.Value.Text);
                break;
            case Result<QuoteSummary, DomainError>.Failure failure:
                PrintError(failure.Error);
                break;
        }
    }

    private static void PrintError(DomainError error) =>
        Console.WriteLine($"[{error.Code}] {error.Message}");
}
