using DeliverySystem.Domain;
namespace DeliverySystem.Processing;
public static class DeferredDeliveryProcessor {
 public static IEnumerable<DeliveryQuote> Stream(IEnumerable<DeliveryRequest> requests,Func<DeliveryRequest,decimal> price) {
  foreach(var request in requests) yield return new DeliveryQuote(request.CustomerName,request.DistanceKm,price(request),request.Category);
 }
 public static DeliveryQuote[] Snapshot(IEnumerable<DeliveryRequest> requests,Func<DeliveryRequest,decimal> price)=>Stream(requests,price).ToArray();
}
