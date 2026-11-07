using DeliverySystem.Domain;
namespace DeliverySystem.Reports;
public sealed record LongDistanceRow(string Customer,double DistanceKm,decimal EstimatedPrice);
public sealed record CategoryTotal(string Category,int Count,decimal Total);
public static class DeliveryReports {
 public static IReadOnlyList<LongDistanceRow> LongDistance(IEnumerable<DeliveryRequest> data,Func<DeliveryRequest,decimal> pricing,double threshold=10)=>
  data.Where(x=>x.DistanceKm>=threshold).Select(x=>new LongDistanceRow(x.CustomerName,x.DistanceKm,pricing(x)))
   .OrderByDescending(x=>x.DistanceKm).ThenBy(x=>x.Customer,StringComparer.Ordinal).ToList();
 public static IReadOnlyList<CategoryTotal> TotalsByCategory(IEnumerable<DeliveryRequest> data,Func<DeliveryRequest,decimal> pricing)=>
  data.GroupBy(x=>x.Category).Select(g=>new CategoryTotal(g.Key,g.Count(),g.Sum(pricing)))
   .OrderByDescending(x=>x.Total).ThenBy(x=>x.Category,StringComparer.Ordinal).ToList();
}
