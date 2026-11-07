namespace DeliverySystem.Domain;
public sealed record DeliveryInput(string CustomerName, double DistanceKm, string Category = "Standard");
public sealed record DeliveryRequest(string CustomerName, double DistanceKm, string Category);
public sealed record DeliveryQuote(string CustomerName, double DistanceKm, decimal Price, string Category);
public sealed record DomainError(string Code, string Message);
public abstract record DeliveryState;
public sealed record PendingDelivery(DeliveryRequest Request) : DeliveryState;
public sealed record QuotedDelivery(DeliveryRequest Request, DeliveryQuote Quote) : DeliveryState;
public sealed record FailedDelivery(string Reason) : DeliveryState;
