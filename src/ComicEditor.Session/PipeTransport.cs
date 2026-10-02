using System.Buffers.Binary;
using System.IO.Pipes;
using System.Text.Json;
using System.Text.Json.Nodes;
using ComicEditor.Core.Validation;
namespace ComicEditor.Session;
public static class PipeTransport
{
    public static async Task WriteAsync(Stream stream,JsonObject packet,int limit,CancellationToken token)
    {
        var bytes=JsonSerializer.SerializeToUtf8Bytes(packet,SessionJson.Options);if(bytes.Length>limit)throw new EditorException("INPUT_LIMIT","Pipe packet exceeds limit");
        var header=new byte[4];BinaryPrimitives.WriteInt32LittleEndian(header,bytes.Length);await stream.WriteAsync(header,token).ConfigureAwait(false);await stream.WriteAsync(bytes,token).ConfigureAwait(false);await stream.FlushAsync(token).ConfigureAwait(false);
    }
    public static async Task<JsonObject> ReadAsync(Stream stream,int limit,CancellationToken token)
    {
        var header=new byte[4];await stream.ReadExactlyAsync(header,token).ConfigureAwait(false);var size=BinaryPrimitives.ReadInt32LittleEndian(header);
        if(size<=0||size>limit)throw new EditorException("INPUT_LIMIT","Invalid pipe packet size");var bytes=new byte[size];await stream.ReadExactlyAsync(bytes,token).ConfigureAwait(false);
        return JsonNode.Parse(bytes,documentOptions:new JsonDocumentOptions{MaxDepth=100})?.AsObject()??throw new EditorException("JSON","Invalid pipe JSON");
    }
    public static async Task<SessionResult> SendAsync(string pipeName,JsonObject request,CancellationToken token)
    {
        if(!pipeName.StartsWith("ComicEditor.Native.",StringComparison.Ordinal)||pipeName.Any(c=>!char.IsAsciiLetterOrDigit(c)&&c!='.'))throw new EditorException("PIPE","Unexpected local pipe name");
        using var client=new NamedPipeClientStream(".",pipeName,PipeDirection.InOut,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
        await client.ConnectAsync(token).ConfigureAwait(false);await WriteAsync(client,request,8*1024*1024,token).ConfigureAwait(false);var packet=await ReadAsync(client,64*1024*1024,token).ConfigureAwait(false);
        return new(packet["envelope"]!.AsObject(),packet["exitCode"]!.GetValue<int>());
    }
}
