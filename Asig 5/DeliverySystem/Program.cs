using System;
using System.Collections.Generic;
using System.Linq;

namespace DeliverySystem;

public sealed class DeliveryRequest
{
    public string CustomerName { get; }
    public double DistanceKm { get; }

    private DeliveryRequest(string customerName, double distanceKm)
    {
        CustomerName = customerName;
        DistanceKm = distanceKm;
    }

    public static DeliveryRequestResult Create(string? customerName, double distanceKm)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(customerName))
            errors.Add("Customer name cannot be empty.");

        if (distanceKm < 0 || distanceKm > 50)
            errors.Add("Distance must be between 0 and 50 km.");

        if (errors.Count > 0)
            return new InvalidDeliveryRequest(errors);

        return new ValidDeliveryRequest(
            new DeliveryRequest(customerName!.Trim(), distanceKm));
    }
}

public abstract record DeliveryRequestResult;
public sealed record ValidDeliveryRequest(DeliveryRequest Value) : DeliveryRequestResult;
public sealed record InvalidDeliveryRequest(IReadOnlyList<string> Errors) : DeliveryRequestResult;

public sealed record DeliveryQuote(string CustomerName, double DistanceKm, decimal Price);

public abstract record DeliveryState;
public sealed record PendingDelivery(DeliveryRequest Request) : DeliveryState;
public sealed record QuotedDelivery(DeliveryRequest Request, DeliveryQuote Quote) : DeliveryState;
public sealed record FailedDelivery(string Reason) : DeliveryState;

public static class DeliveryService
{
    public static List<DeliveryRequest> SelectValidRequests(List<DeliveryRequest> requests)
    {
        ArgumentNullException.ThrowIfNull(requests);
        return requests.ToList();
    }

    public static List<DeliveryQuote> CalculatePrices(
        List<DeliveryRequest> requests,
        Func<DeliveryRequest, decimal> pricePolicy)
    {
        ArgumentNullException.ThrowIfNull(requests);
        ArgumentNullException.ThrowIfNull(pricePolicy);

        return requests.Select(request => new DeliveryQuote(
            request.CustomerName, request.DistanceKm, pricePolicy(request))).ToList();
    }

    public static List<DeliveryQuote> SortByPrice(List<DeliveryQuote> quotes)
    {
        ArgumentNullException.ThrowIfNull(quotes);
        return quotes.OrderBy(quote => quote.Price).ToList();
    }

    public static List<DeliveryQuote> RunPipeline(
        List<DeliveryRequest> requests,
        Func<DeliveryRequest, decimal> pricePolicy)
    {
        var validRequests = SelectValidRequests(requests);
        return SortByPrice(CalculatePrices(validRequests, pricePolicy));
    }

    public static DeliveryState CalculateQuote(
        DeliveryState state,
        Func<DeliveryRequest, decimal> pricePolicy)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(pricePolicy);

        return state switch
        {
            PendingDelivery pending => new QuotedDelivery(
                pending.Request,
                new DeliveryQuote(
                    pending.Request.CustomerName,
                    pending.Request.DistanceKm,
                    pricePolicy(pending.Request))),
            QuotedDelivery => new FailedDelivery("This delivery has already been quoted."),
            FailedDelivery failed => failed,
            _ => new FailedDelivery("Unknown delivery state.")
        };
    }

    public static decimal StandardPrice(DeliveryRequest request) =>
        500m + 100m * (decimal)request.DistanceKm;

    public static decimal ExpressPrice(DeliveryRequest request) =>
        800m + 150m * (decimal)request.DistanceKm;
}

public static class Program
{
    public static void Main()
    {
        var requests = new List<DeliveryRequest>();
        AddRequest("Aruzhan", 2, requests);
        AddRequest("Dias", 5, requests);
        AddRequest("Ali", 0, requests);
        AddRequest("Dana", 3, requests);
        AddRequest("Miras", 1, requests);

        Console.WriteLine("STANDARD DELIVERY:");
        ShowQuotes(DeliveryService.RunPipeline(requests, DeliveryService.StandardPrice));

        Console.WriteLine();
        Console.WriteLine("EXPRESS DELIVERY:");
        ShowQuotes(DeliveryService.RunPipeline(requests, DeliveryService.ExpressPrice));

        Console.WriteLine();
        Console.WriteLine("FACTORY REJECTION:");
        var invalid = DeliveryRequest.Create(" ", -1);
        Console.WriteLine(invalid switch
        {
            ValidDeliveryRequest => "Request accepted.",
            InvalidDeliveryRequest bad => string.Join(" ", bad.Errors),
            _ => "Unexpected result."
        });

        Console.WriteLine();
        Console.WriteLine("SAFE WORKFLOW:");
        var creation = DeliveryRequest.Create("Test Customer", 2);
        if (creation is ValidDeliveryRequest valid)
        {
            DeliveryState first = DeliveryService.CalculateQuote(
                new PendingDelivery(valid.Value), DeliveryService.StandardPrice);
            Console.WriteLine(first is QuotedDelivery ? "Quote created." : "Quote failed.");

            DeliveryState repeated = DeliveryService.CalculateQuote(
                first, DeliveryService.StandardPrice);
            Console.WriteLine(repeated is FailedDelivery failed
                ? failed.Reason
                : "Unexpected result.");
        }
    }

    private static void AddRequest(string name, double distance, List<DeliveryRequest> requests)
    {
        switch (DeliveryRequest.Create(name, distance))
        {
            case ValidDeliveryRequest valid:
                requests.Add(valid.Value);
                break;
            case InvalidDeliveryRequest invalid:
                Console.WriteLine($"Skipped request: {string.Join(", ", invalid.Errors)}");
                break;
            default:
                throw new InvalidOperationException("Unknown factory result.");
        }
    }

    private static void ShowQuotes(List<DeliveryQuote> quotes)
    {
        foreach (var quote in quotes)
            Console.WriteLine($"{quote.CustomerName} | {quote.DistanceKm} km | {quote.Price} KZT");
    }
}