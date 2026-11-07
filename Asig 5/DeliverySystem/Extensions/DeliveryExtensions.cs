using DeliverySystem.Domain;
using DeliverySystem.Functional;
namespace DeliverySystem.Extensions;
public static class DeliveryExtensions {
 public static IEnumerable<T> WhereDistanceAtLeast<T>(this IEnumerable<T> source,double min,Func<T,double> distance)=>source.Where(x=>distance(x)>=min);
 public static Result<U,E> MapFailureAware<T,U,E>(this Result<T,E> result,Func<T,U> transform)=>result.Map(transform);
}
