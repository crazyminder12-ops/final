using DeliverySystem.Domain;
using DeliverySystem.Functional;
namespace DeliverySystem.Services;
public static class DeliveryValidator {
 public static Result<DeliveryRequest,DomainError> Validate(DeliveryInput input) {
  if(string.IsNullOrWhiteSpace(input.CustomerName)) return new Result<DeliveryRequest,DomainError>.Failure(new("INVALID_CUSTOMER","Customer name cannot be empty."));
  if(double.IsNaN(input.DistanceKm)||double.IsInfinity(input.DistanceKm)||input.DistanceKm<0||input.DistanceKm>50) return new Result<DeliveryRequest,DomainError>.Failure(new("INVALID_DISTANCE","Distance must be between 0 and 50 km."));
  return new Result<DeliveryRequest,DomainError>.Success(new(input.CustomerName.Trim(),input.DistanceKm,input.Category));
 }
}
