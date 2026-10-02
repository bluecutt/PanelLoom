using System.Text.Json.Nodes;
using ComicEditor.Core.AppState;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
using ComicEditor.Session;
namespace ComicEditor.Cli;
public static class SessionCommands
{
    public static CliResult Execute(CliArguments options,string requestId,CancellationToken token)
    {
        var directory=options.Get("registry-dir")??Path.Combine(StatePaths.RootFor(AppContext.BaseDirectory,options.Has("portable-data")),"Sessions");var registrations=new SessionRegistry(directory).List();
        if(options.Subcommand=="list")return new(JsonOutput.Envelope(requestId,true,"OK","Local sessions",new JsonObject{["sessions"]=JsonOutput.Node(registrations),["registryDirectory"]=Path.GetFullPath(directory)}),0);
        var id=options.Required("session");var registration=registrations.FirstOrDefault(r=>r.SessionId==id)??throw new EditorException("SESSION_NOT_FOUND","No current-user session matches this ID",3);
        var request=new JsonObject{["apiVersion"]=1,["requestId"]=requestId,["sessionId"]=id,["kind"]=options.Subcommand,["timeoutSeconds"]=options.Number("timeout",30)};
        if(options.Subcommand=="apply")
        {
            var patch=FileTransactionService.ReadPatch(options.Required("patch"));if(patch.RequestId!=requestId||patch.BaseRevision is null)throw new EditorException("ARGS","Live patch needs matching requestId and baseRevision");
            request["baseRevision"]=patch.BaseRevision;request["baseProjectHash"]=patch.BaseProjectHash;request["dryRun"]=options.Has("dry-run");request["operations"]=JsonOutput.Node(patch.Operations.Select(op=>new{op=op.Op,targetId=op.TargetId,args=op.Args}));
        }
        if(options.Subcommand=="save")
        {
            var revision=options.Number("revision",-1);if(revision<0||revision!=Math.Truncate(revision))throw new EditorException("ARGS","Current integer --revision required");
            request["baseRevision"]=revision;request["baseProjectHash"]=options.Required("base-hash");request["savePath"]=Path.GetFullPath(options.Required("out"));if(options.Get("if-output-hash") is {} hash)request["expectedOutputHash"]=hash;
        }
        var result=PipeTransport.SendAsync(registration.PipeName,request,token).GetAwaiter().GetResult();return new(result.Envelope,result.ExitCode);
    }
}
