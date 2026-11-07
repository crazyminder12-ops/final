using DeliverySystem.Domain;
namespace DeliverySystem.Services;
public static class PricingService {
 public static decimal Standard(DeliveryRequest r)=>500m+100m*(decimal)r.DistanceKm;
 public static decimal Express(DeliveryRequest r)=>800m+150m*(decimal)r.DistanceKm;
 public static DeliveryQuote Quote(DeliveryRequest r,Func<DeliveryRequest,decimal> policy)=>new(r.CustomerName,r.DistanceKm,policy(r),r.Category);
 public static DeliveryState Transition(DeliveryState state,Func<DeliveryRequest,decimal> policy)=>state switch {
  PendingDelivery p=>new QuotedDelivery(p.Request,Quote(p.Request,policy)),
  QuotedDelivery=>new FailedDelivery("This delivery has already been quoted."),
  FailedDelivery f=>f,_=>new FailedDelivery("Unknown delivery state.")
 };
}
