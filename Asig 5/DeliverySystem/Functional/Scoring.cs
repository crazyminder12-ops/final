using DeliverySystem.Domain;
namespace DeliverySystem.Functional;
public sealed record ScoreConfiguration(decimal BaseWeight,decimal DistanceWeight);
public static class Scoring {
 public static Func<DeliveryRequest,decimal> CreateScore(ScoreConfiguration config)=>request=>config.BaseWeight+config.DistanceWeight*(decimal)request.DistanceKm;
 public static Func<DeliveryRequest,decimal> Economy=CreateScore(new(100m,10m));
 public static Func<DeliveryRequest,decimal> Priority=CreateScore(new(250m,25m));
}
