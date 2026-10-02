using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
namespace ComicEditor.Session;
public static class SessionJson
{
    public static JsonSerializerOptions Options {get;}=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
    public static JsonNode? Node<T>(T value)=>JsonSerializer.SerializeToNode(value,Options);
    public static SessionResult Result(string? requestId,bool success,string code,string message,JsonNode? data=null,int exitCode=0,JsonNode? changed=null,JsonNode? warnings=null)=>new(new(){["apiVersion"]=1,["requestId"]=requestId,["success"]=success,["code"]=code,["message"]=message,["data"]=data??new JsonObject(),["changedObjects"]=changed??new JsonArray(),["warnings"]=warnings??new JsonArray()},exitCode);
}
