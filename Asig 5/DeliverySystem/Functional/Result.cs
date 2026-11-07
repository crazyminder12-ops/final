namespace DeliverySystem.Functional;
public abstract record Result<T,E> {
 public sealed record Success(T Value):Result<T,E>;
 public sealed record Failure(E Error):Result<T,E>;
 public Result<U,E> Map<U>(Func<T,U> f) => this switch {
  Success s => new Result<U,E>.Success(f(s.Value)),
  Failure e => new Result<U,E>.Failure(e.Error), _ => throw new InvalidOperationException()
 };
 public Result<U,E> Bind<U>(Func<T,Result<U,E>> f) => this switch {
  Success s => f(s.Value), Failure e => new Result<U,E>.Failure(e.Error), _ => throw new InvalidOperationException()
 };
}
