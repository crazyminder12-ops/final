using System.Runtime.InteropServices;
using DeliverySystem.Domain;
using DeliverySystem.Functional;
namespace DeliverySystem.Native;
public sealed class NativeFee {
 public Result<int,DomainError> Calculate(int distanceMeters,int rateCentsPerKm) {
  if(distanceMeters<0||distanceMeters>50000||rateCentsPerKm<0) return new Result<int,DomainError>.Failure(new("NATIVE_INVALID_INPUT","Distance must be 0..50000 metres and rate cannot be negative."));
  try { return new Result<int,DomainError>.Success(NativeMethods.DeliverySurchargeCents(distanceMeters,rateCentsPerKm)); }
  catch(DllNotFoundException ex){return new Result<int,DomainError>.Failure(new("NATIVE_LIBRARY_NOT_FOUND",ex.Message));}
  catch(EntryPointNotFoundException ex){return new Result<int,DomainError>.Failure(new("NATIVE_ENTRY_NOT_FOUND",ex.Message));}
 }
}
