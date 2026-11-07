using DeliverySystem.Domain; using DeliverySystem.Functional; using DeliverySystem.IO; using DeliverySystem.Native; using Xunit;
public class NativeAndFileTests {
 [Fact] public void NativeAdapterRejectsInvalidInputBeforeInterop(){var x=new NativeFee().Calculate(-1,10);Assert.Equal("NATIVE_INVALID_INPUT",Assert.IsType<Result<int,DomainError>.Failure>(x).Error.Code);}
 [Fact] public void MissingFileIsResultFailure(){var x=DeliveryFileStore.Load(Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".json"));Assert.Equal("FILE_NOT_FOUND",Assert.IsType<Result<List<DeliveryInput>,DomainError>.Failure>(x).Error.Code);}
 [Fact] public void MalformedJsonIsResultFailure(){var p=Path.GetTempFileName();try{File.WriteAllText(p,"{ bad");var x=DeliveryFileStore.Load(p);Assert.Equal("MALFORMED_JSON",Assert.IsType<Result<List<DeliveryInput>,DomainError>.Failure>(x).Error.Code);}finally{File.Delete(p);}}
 [Fact] public void ValidJsonLoads(){var p=Path.GetTempFileName();try{File.WriteAllText(p,"""[{"CustomerName":"A","DistanceKm":1}]""");Assert.Single(Assert.IsType<Result<List<DeliveryInput>,DomainError>.Success>(DeliveryFileStore.Load(p)).Value);}finally{File.Delete(p);}}
}
