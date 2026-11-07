namespace DeliverySystem.Functional;
public abstract record Option<T> {
 public sealed record Some(T Value):Option<T>;
 public sealed record None:Option<T>;
}
