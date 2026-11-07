using System.Text.Json;
using DeliverySystem.Domain;
using DeliverySystem.Functional;
namespace DeliverySystem.IO;
public static class DeliveryFileStore {
 public static Result<List<DeliveryInput>,DomainError> Load(string path) {
  try {
   using var stream=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read);
   var data=JsonSerializer.Deserialize<List<DeliveryInput>>(stream);
   return data is null ? new Result<List<DeliveryInput>,DomainError>.Failure(new("EMPTY_JSON","JSON did not contain a delivery list.")) : new Result<List<DeliveryInput>,DomainError>.Success(data);
  } catch(FileNotFoundException){return new Result<List<DeliveryInput>,DomainError>.Failure(new("FILE_NOT_FOUND",$"File not found: {path}"));}
    catch(JsonException ex){return new Result<List<DeliveryInput>,DomainError>.Failure(new("MALFORMED_JSON",ex.Message));}
    catch(IOException ex){return new Result<List<DeliveryInput>,DomainError>.Failure(new("FILE_READ_ERROR",ex.Message));}
 }
 public static Result<bool,DomainError> Export(string path,IEnumerable<DeliveryQuote> quotes) {
  try { using var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None); JsonSerializer.Serialize(stream,quotes,new JsonSerializerOptions{WriteIndented=true}); return new Result<bool,DomainError>.Success(true); }
  catch(IOException ex){return new Result<bool,DomainError>.Failure(new("FILE_WRITE_ERROR",ex.Message));}
 }
}
