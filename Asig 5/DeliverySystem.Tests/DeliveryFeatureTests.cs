using DeliverySystem.Domain; using DeliverySystem.Extensions; using DeliverySystem.Functional; using DeliverySystem.Processing; using DeliverySystem.Reports; using DeliverySystem.Services; using Xunit;
public class DeliveryFeatureTests {
 private static DeliveryRequest R(string n,double d,string c="Standard")=>new(n,d,c);
 [Fact] public void ExtensionFiltersDistance(){Assert.Single(new[]{R("A",2),R("B",12)}.WhereDistanceAtLeast(10,x=>x.DistanceKm));}
 [Fact] public void ExtensionMapsResult(){var x=new Result<int,string>.Success(3).MapFailureAware(v=>v*2);Assert.Equal(6,Assert.IsType<Result<int,string>.Success>(x).Value);}
 [Fact] public void LongDistanceReportFiltersAndOrdersTiesByName(){var x=DeliveryReports.LongDistance(new[]{R("Z",12),R("A",12),R("B",2)},_=>100);Assert.Equal(new[]{"A","Z"},x.Select(v=>v.Customer));}
 [Fact] public void ReportsHandleEmptyInput(){Assert.Empty(DeliveryReports.LongDistance(Array.Empty<DeliveryRequest>(),_=>0));}
 [Fact] public void ReportHandlesNoMatches(){Assert.Empty(DeliveryReports.LongDistance(new[]{R("A",1)},_=>0));}
 [Fact] public void CategoryReportGroupsAndSums(){var x=DeliveryReports.TotalsByCategory(new[]{R("A",2,"X"),R("B",3,"X"),R("C",1,"Y")},r=>(decimal)r.DistanceKm);Assert.Equal(5m,x[0].Total);Assert.Equal(2,x[0].Count);}
 [Fact] public void CurriedConfigurationsDifferOnSameRequest(){var r=R("A",10);Assert.NotEqual(Scoring.Economy(r),Scoring.Priority(r));}
 [Fact] public void DeferredQueryDoesNotRunBeforeEnumeration(){int count=0;IEnumerable<int> Q(){foreach(var i in new[]{1,2}){count++;yield return i;}}var q=Q();Assert.Equal(0,count);_ = q.ToArray();Assert.Equal(2,count);}
 [Fact] public void SnapshotCanBeReusedWithoutReprocessing(){int count=0;var q=DeferredDeliveryProcessor.Stream(new[]{R("A",1)},r=>{count++;return 600;}).ToArray();_ = q.ToArray();Assert.Equal(1,count);}
 [Fact] public void ValidatorRejectsInvalidDistance(){var x=DeliveryValidator.Validate(new("A",51));Assert.Equal("INVALID_DISTANCE",Assert.IsType<Result<DeliveryRequest,DomainError>.Failure>(x).Error.Code);}
 [Fact] public void ValidatorRejectsBlankCustomer(){Assert.IsType<Result<DeliveryRequest,DomainError>.Failure>(DeliveryValidator.Validate(new(" ",1)));}
 [Fact] public void LookupReturnsSome(){Assert.IsType<Option<DeliveryRequest>.Some>(DeliveryLookup.Find(new[]{R("A",1)},"a"));}
}
