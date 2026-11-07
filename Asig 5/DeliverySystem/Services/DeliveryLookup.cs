using DeliverySystem.Domain;
using DeliverySystem.Functional;
namespace DeliverySystem.Services;
public static class DeliveryLookup {
 public static Option<DeliveryRequest> Find(IEnumerable<DeliveryRequest> requests,string customer) {
  var found=requests.FirstOrDefault(x=>string.Equals(x.CustomerName,customer,StringComparison.OrdinalIgnoreCase));
  return found is null ? new Option<DeliveryRequest>.None() : new Option<DeliveryRequest>.Some(found);
 }
 public static Result<DeliveryRequest,DomainError> Require(Option<DeliveryRequest> value)=>value switch {
  Option<DeliveryRequest>.Some s=>new Result<DeliveryRequest,DomainError>.Success(s.Value),
  _=>new Result<DeliveryRequest,DomainError>.Failure(new("DELIVERY_NOT_FOUND","No delivery found for this customer."))
 };
}
