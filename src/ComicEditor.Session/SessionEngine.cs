using System.Text.Json.Nodes;
using System.Text;
using System.Security.Cryptography;
using ComicEditor.Core.Project;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Validation;
namespace ComicEditor.Session;
public sealed class SessionEngine(string id,IEditorSession editor,int capacity=10000)
{
    private readonly SemaphoreSlim serial=new(1,1);
    private readonly RequestLedger ledger=new(capacity);
    public async Task<SessionResult> HandleAsync(JsonObject request,CancellationToken token=default)
    {
        string? requestId=null;var acquired=false;
        try
        {
            request=request.DeepClone().AsObject();requestId=request.Text("requestId");var payload=Encoding.UTF8.GetBytes(request.ToJsonString());
            if(payload.Length>8*1024*1024)throw new EditorException("INPUT_LIMIT","Session request exceeds 8 MiB");
            if(request.Any(p=>p.Key is not("apiVersion" or "requestId" or "sessionId" or "kind" or "baseRevision" or "baseProjectHash" or "operations" or "savePath" or "expectedOutputHash" or "dryRun" or "timeoutSeconds"))||request.Number("apiVersion")!=1||!Guid.TryParse(requestId,out _))throw new EditorException("ARGS","Invalid API1 session request");
            if(request.Text("sessionId")!=id)throw new EditorException("CONFLICT","Session ID differs",4);
            var kind=request.Text("kind");if(kind is not("status" or "snapshot" or "apply" or "save"))throw new EditorException("ARGS","Unknown request kind");
            if(!serial.Wait(0))throw new EditorException("BUSY","Another session request is active",6);acquired=true;token.ThrowIfCancellationRequested();
            var semantic=request.DeepClone().AsObject();semantic.Remove("timeoutSeconds");var digest=Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(semantic.ToJsonString())));var prior=ledger.Find(requestId,digest);if(prior is not null)return prior;
            if(kind is "apply" or "save"&&editor.Busy)throw new EditorException("BUSY","Editor interaction is busy",6);
            return await editor.InvokeAsync(()=>
            {
                token.ThrowIfCancellationRequested();var doc=editor.Snapshot();var hash=SessionHash.Compute(doc);
                var data=new JsonObject{["sessionId"]=id,["revision"]=editor.Revision,["hash"]=hash,["busy"]=editor.Busy,["allowWrite"]=editor.AllowWrite,["projectPath"]=doc.SourcePath};
                if(kind is "status" or "snapshot") {if(kind=="snapshot")data["project"]=doc.Root.DeepClone();return SessionJson.Result(requestId,true,"OK","Live state",data);}
                if(editor.Busy)throw new EditorException("BUSY","Editor interaction became busy",6);
                if(!editor.AllowWrite)throw new EditorException("READ_ONLY","Enable Agent writes explicitly in the GUI",5);
                if(request.Number("baseRevision",-1)!=editor.Revision||request.Text("baseProjectHash")!=hash)throw new EditorException("CONFLICT","Live revision or hash is stale; fetch a fresh snapshot",4);
                if(kind=="apply")
                {
                    var patch=FileTransactionService.ParsePatch(new(){["apiVersion"]=1,["requestId"]=requestId,["baseProjectHash"]=hash,["baseRevision"]=editor.Revision,["operations"]=request["operations"]?.DeepClone()});
                    var plan=new CommandProcessor().Plan(doc,patch.Operations);data["changes"]=SessionJson.Node(plan.Changes);data["hash"]=SessionHash.Compute(plan.After);data["outcomes"]=SessionJson.Node(plan.Outcomes??[]);
                    if(request.Flag("dryRun")){data["dryRun"]=true;data["project"]=plan.After.Root.DeepClone();return SessionJson.Result(requestId,true,"OK","Dry run",data,changed:SessionJson.Node(plan.Changes.Select(c=>c.ObjectId).Distinct()),warnings:SessionJson.Node(plan.Warnings));}
                    data["revision"]=editor.Revision+(plan.Changes.Count>0?1:0);var predicted=SessionJson.Result(requestId,true,"OK","Applied as one undo batch",data,changed:SessionJson.Node(plan.Changes.Select(c=>c.ObjectId).Distinct()),warnings:SessionJson.Node(plan.Warnings));ledger.EnsureCapacity(predicted);
                    token.ThrowIfCancellationRequested();editor.Execute(patch.Operations);data["revision"]=editor.Revision;data["hash"]=SessionHash.Compute(editor.Snapshot());ledger.Add(requestId,digest,predicted);return predicted;
                }
                var path=request.Text("savePath");if(string.IsNullOrWhiteSpace(path))throw new EditorException("ARGS","savePath required");data["path"]=Path.GetFullPath(path);data["savedHash"]=new string('0',64);data["revision"]=editor.Revision+1;
                var savedResult=SessionJson.Result(requestId,true,"OK","Saved live state",data);ledger.EnsureCapacity(savedResult);token.ThrowIfCancellationRequested();
                var saved=editor.Save(path,request["expectedOutputHash"]?.GetValue<string>());data["path"]=saved.AbsolutePath;data["savedHash"]=saved.Sha256;data["revision"]=editor.Revision;data["hash"]=SessionHash.Compute(editor.Snapshot());ledger.Add(requestId,digest,savedResult);return savedResult;
            },token).ConfigureAwait(false);
        }
        catch(EditorException ex){return SessionJson.Result(requestId,false,ex.Code,ex.Message,exitCode:ex.ExitCode);}
        catch(OperationCanceledException){return SessionJson.Result(requestId,false,"CANCELLED_OR_TIMEOUT","Request cancelled before its next commit point",exitCode:6);}
        catch(UnauthorizedAccessException ex){return SessionJson.Result(requestId,false,"PERMISSION",ex.Message,exitCode:5);}
        catch(IOException ex){return SessionJson.Result(requestId,false,"IO",ex.Message,exitCode:3);}
        catch(Exception ex)when(ex is System.Text.Json.JsonException or InvalidOperationException or FormatException or ArgumentException){return SessionJson.Result(requestId,false,"ARGS",ex.Message,exitCode:2);}
        catch(Exception ex){return SessionJson.Result(requestId,false,"INTERNAL",ex.Message,exitCode:70);}
        finally{if(acquired)serial.Release();}
    }
}
