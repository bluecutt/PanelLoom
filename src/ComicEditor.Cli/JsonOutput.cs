using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Encodings.Web;
namespace ComicEditor.Cli;
public static class JsonOutput
{
    public static JsonSerializerOptions Options { get; }=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,Encoder=JavaScriptEncoder.UnsafeRelaxedJsonEscaping};
    public static JsonNode? Node<T>(T value)=>JsonSerializer.SerializeToNode(value,Options);
    public static JsonObject Envelope(string? id,bool success,string code,string message,JsonNode? data=null,JsonNode? changed=null,JsonNode? warnings=null)=>new(){["apiVersion"]=1,["requestId"]=id,["success"]=success,["code"]=code,["message"]=message,["data"]=data??new JsonObject(),["changedObjects"]=changed??new JsonArray(),["warnings"]=warnings??new JsonArray()};
}
