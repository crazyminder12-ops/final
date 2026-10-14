using System;
using System.Collections.Generic;
using System.Linq;

namespace DeliverySystem;

public class DeliveryRequest
{
    public string CustomerName { get; }
    public double DistanceKm { get; }

    public DeliveryRequest(string customerName, double distanceKm)
    {
        if (string.IsNullOrWhiteSpace(customerName))
            throw new ArgumentException("Customer name cannot be empty.");

        if (distanceKm < 0)
            throw new ArgumentException("Distance cannot be negative.");

        CustomerName = customerName;
        DistanceKm = distanceKm;
    }
}

public class DeliveryQuote
{
    public string CustomerName { get; }
    public double DistanceKm { get; }
    public decimal Price { get; }

    public DeliveryQuote(string customerName, double distanceKm, decimal price)
    {
        CustomerName = customerName;
        DistanceKm = distanceKm;
        Price = price;
    }
}

public class DeliveryService
{
    // select requests inside the delivery area (up to 50 km).
    public static List<DeliveryRequest> SelectValidRequests(List<DeliveryRequest> requests)
    {
        if (requests == null)
            throw new ArgumentNullException(nameof(requests));

        return requests.Where(request => request.DistanceKm <= 50).ToList();
    }

    // calculate delivery prices using a policy passed to this method.
    public static List<DeliveryQuote> CalculatePrices(
        List<DeliveryRequest> requests,
        Func<DeliveryRequest, decimal> pricePolicy)
    {
        if (requests == null)
            throw new ArgumentNullException(nameof(requests));

        if (pricePolicy == null)
            throw new ArgumentNullException(nameof(pricePolicy));

        return requests
            .Select(request => new DeliveryQuote(
                request.CustomerName,
                request.DistanceKm,
                pricePolicy(request)))
            .ToList();
    }

    // sort the cheapest delivery first.
    public static List<DeliveryQuote> SortByPrice(List<DeliveryQuote> quotes)
    {
        if (quotes == null)
            throw new ArgumentNullException(nameof(quotes));

        return quotes.OrderBy(quote => quote.Price).ToList();
    }

    // Run all three stages in order.
    public static List<DeliveryQuote> RunPipeline(
        List<DeliveryRequest> requests,
        Func<DeliveryRequest, decimal> pricePolicy)
    {
        List<DeliveryRequest> validRequests = SelectValidRequests(requests);
        List<DeliveryQuote> quotes = CalculatePrices(validRequests, pricePolicy);
        return SortByPrice(quotes);
    }

    // Policy 1: standard delivery costs 500 + 100 per kilometre.
    public static decimal StandardPrice(DeliveryRequest request)
    {
        return 500m + 100m * (decimal)request.DistanceKm;
    }

    // Policy 2: express delivery costs 800 + 150 per kilometre.
    public static decimal ExpressPrice(DeliveryRequest request)
    {
        return 800m + 150m * (decimal)request.DistanceKm;
    }
}

public class Program
{
    public static void Main()
    {
        List<DeliveryRequest> requests = new List<DeliveryRequest>
        {
            new DeliveryRequest("Aruzhan", 2),
            new DeliveryRequest("Dias", 5),
            new DeliveryRequest("Ali", 0),
            new DeliveryRequest("Dana", 3),
            new DeliveryRequest("Miras", 1),
            new DeliveryRequest("Outside area", 60)
        };

        Console.WriteLine("STANDARD DELIVERY:");
        ShowQuotes(DeliveryService.RunPipeline(requests, DeliveryService.StandardPrice));

        Console.WriteLine();
        Console.WriteLine("EXPRESS DELIVERY:");
        ShowQuotes(DeliveryService.RunPipeline(requests, DeliveryService.ExpressPrice));

        Console.WriteLine();
        Console.WriteLine("EMPTY DATASET:");
        var emptyResult = DeliveryService.RunPipeline(
            new List<DeliveryRequest>(), DeliveryService.StandardPrice);
        Console.WriteLine("Quotes found: " + emptyResult.Count);
    }

    private static void ShowQuotes(List<DeliveryQuote> quotes)
    {
        foreach (DeliveryQuote quote in quotes)
        {
            Console.WriteLine(
                quote.CustomerName + " | " +
                quote.DistanceKm + " km | " +
                quote.Price + " KZT");
        }
    }
}
