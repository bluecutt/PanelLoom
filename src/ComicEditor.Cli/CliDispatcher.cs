using System.Text.Json;
using System.Text.Json.Nodes;
using ComicEditor.Core.Project;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Validation;
using ComicEditor.Rendering;
namespace ComicEditor.Cli;
public sealed record CliResult(JsonObject Envelope,int ExitCode);
public sealed class CliDispatcher(Action? beforeDispatch=null)
{
    public async Task<CliResult> RunAsync(string[] args,CancellationToken token=default)
    {
        string? requestId=null; CliArguments? options=null;
        try
        {
            options=CliParser.Parse(args); requestId=options.Get("request-id")??Guid.NewGuid().ToString();
            CheckPathRoles(options);
            var seconds=options.Number("timeout",30); if(seconds<=0||seconds>86400) throw new EditorException("ARGS","timeout must be positive and at most 86400 seconds");
            using var deadline=CancellationTokenSource.CreateLinkedTokenSource(token); deadline.CancelAfter(TimeSpan.FromSeconds(seconds));
            if(!options.Has("help")&&(options.Command=="apply"||(options.Command=="session"&&options.Subcommand=="apply")))
            {
                var patch=FileTransactionService.ReadPatch(options.Required("patch"));
                if(options.Has("request-id")&&options.Get("request-id")!=patch.RequestId) throw new EditorException("ARGS","CLI and patch request IDs differ"); requestId=patch.RequestId;
            }
            if(options.Get("report") is { } report&&!options.Has("dry-run")) AtomicFile.CheckDestination(report,false,null);
            var response=await Task.Run(()=> { deadline.Token.ThrowIfCancellationRequested(); beforeDispatch?.Invoke(); deadline.Token.ThrowIfCancellationRequested(); return Execute(options,requestId,deadline.Token); },deadline.Token).ConfigureAwait(false);
            if(options.Get("report") is { } reportPath&&!options.Has("dry-run"))
            {
                try { AtomicFile.Write(reportPath,false,null,stream=>JsonSerializer.Serialize(stream,response.Envelope,JsonOutput.Options)); }
                catch(Exception ex) when(ex is EditorException or IOException or UnauthorizedAccessException)
                { response.Envelope["warnings"]!.AsArray().Add(new JsonObject{["code"]="REPORT_NOT_WRITTEN",["message"]=ex.Message,["path"]=Path.GetFullPath(reportPath)}); }
            }
            return response;
        }
        catch(EditorException ex) { return new(JsonOutput.Envelope(requestId,false,ex.Code,ex.Message),ex.ExitCode); }
        catch(OperationCanceledException) { return new(JsonOutput.Envelope(requestId,false,options?.Command=="session"?"SESSION_RESULT_UNKNOWN":"CANCELLED_OR_TIMEOUT",options?.Command=="session"?"Remote result may be unknown: inspect the live snapshot or retry the identical request ID; never assume a rollback":"Request cancelled or timed out; no partial output committed"),6); }
        catch(UnauthorizedAccessException ex) { return new(JsonOutput.Envelope(requestId,false,"PERMISSION",ex.Message),5); }
        catch(IOException ex) { return new(JsonOutput.Envelope(requestId,false,"IO",ex.Message),3); }
        catch(Exception ex) when(ex is JsonException or ArgumentException or FormatException or InvalidOperationException) { return new(JsonOutput.Envelope(requestId,false,"ARGS_OR_JSON",ex.Message),2); }
        catch(Exception ex) { return new(JsonOutput.Envelope(requestId,false,"INTERNAL",ex.Message),70); }
    }
    private static void CheckPathRoles(CliArguments options)
    {
        static bool Same(string a,string b)=>string.Equals(Path.GetFullPath(a),Path.GetFullPath(b),StringComparison.OrdinalIgnoreCase);
        var paths=new[]{"project","patch","manifest","out"}.Select(options.Get).Where(p=>p is not null).Cast<string>().ToArray();
        if(options.Get("report") is { } report&&paths.Any(p=>Same(p,report))) throw new EditorException("ARGS","Report path must differ from all input and output paths");
        if(options.Get("out") is { } output)
        {
            foreach(var key in new[]{"patch","manifest"}) if(options.Get(key) is { } input&&Same(input,output)) throw new EditorException("ARGS","Output must differ from request input");
            if(options.Command=="render"&&options.Get("project") is { } project&&Same(project,output)) throw new EditorException("ARGS","PNG output cannot overwrite the project");
        }
    }
    private static CliResult Execute(CliArguments options,string requestId,CancellationToken token)
    {
        if(options.Command=="help"||options.Has("help"))return new(JsonOutput.Envelope(requestId,true,"OK","Command help",CliHelp.Data()),0);
        JsonNode? data; IReadOnlyList<Change> changes=[]; IReadOnlyList<Issue> warnings=[];
        switch(options.Command)
        {
            case "capabilities":
                data=new JsonObject{["projectFormat"]="ComicPanelEditorProject",["projectVersion"]=2,["actions"]=JsonOutput.Node(CommandRegistry.Actions),["commands"]=JsonOutput.Node(new[]{"capabilities","inspect","validate","init","apply","render","bundle","session","open","help"}),["help"]=CliHelp.Data(),["guiOpenSupported"]=true,["sessionSupported"]=true,["bundleSupported"]=true,["maxPatchBytes"]=8*1024*1024,["maxOperations"]=1000}; break;
            case "inspect":
                var inspected=new ProjectStore().Load(options.Required("project")); data=new JsonObject{["path"]=inspected.AbsolutePath,["hash"]=inspected.Sha256,["project"]=inspected.Document.Root.DeepClone(),["assets"]=JsonOutput.Node(new AssetResolver().Inspect(inspected.Document)),["quality"]=JsonOutput.Node(QualityAssessment.Assess(inspected.Document,new ProjectView(inspected.Document).Scale))}; break;
            case "validate":
                var validated=new ProjectStore().Load(options.Required("project")); var issues=ProjectValidator.Validate(validated.Document).ToList();
                foreach(var asset in new AssetResolver().Inspect(validated.Document).Where(a=>a.Error is not null)) issues.Add(new("error","ASSET_IO",asset.ObjectId,asset.Reference,asset.Error!));
                var valid=issues.All(i=>i.Severity!="error"); return new(JsonOutput.Envelope(requestId,valid,valid?"OK":"ASSET_IO",valid?"Project valid":"Source assets invalid",new JsonObject{["valid"]=valid,["hash"]=validated.Sha256,["issues"]=JsonOutput.Node(issues)},warnings:JsonOutput.Node(issues.Where(i=>i.Severity=="warning"))),valid?0:3);
            case "init":
                var initialized=ProjectInitializer.Create(options.Required("manifest")); token.ThrowIfCancellationRequested(); var initSaved=new ProjectStore(_=>token.ThrowIfCancellationRequested()).Save(initialized,options.Required("out"),new()); data=JsonOutput.Node(new{path=initSaved.AbsolutePath,hash=initSaved.Sha256}); break;
            case "apply":
                var source=new ProjectStore().Load(options.Required("project")); var patch=FileTransactionService.ReadPatch(options.Required("patch"));
                if(patch.RequestId!=requestId) throw new EditorException("CONFLICT","Patch request ID changed during read",4);
                var plan=FileTransactionService.Plan(source,patch,token); changes=plan.Changes; warnings=plan.Warnings;
                if(options.Has("dry-run")) data=new JsonObject{["dryRun"]=true,["baseProjectHash"]=source.Sha256,["changes"]=JsonOutput.Node(changes),["project"]=plan.After.Root.DeepClone()};
                else { var saved=FileTransactionService.Save(source,plan,options.Required("out"),new(options.Has("overwrite"),options.Get("if-output-hash")),token); data=new JsonObject{["path"]=saved.AbsolutePath,["hash"]=saved.Sha256,["changes"]=JsonOutput.Node(changes)}; }
                data!["outcomes"]=JsonOutput.Node(plan.Outcomes??[]);
                break;
            case "render":
                var renderSource=new ProjectStore().Load(options.Required("project")); var render=new PageRenderer().Render(renderSource.Document,options.Required("out"),new(options.Number("scale",0),options.Get("object"),options.Number("padding",0),checked((long)(options.Number("memory-limit-mib",1024)*1024*1024)),options.Has("overwrite"),options.Get("if-output-hash")),token); data=JsonOutput.Node(render); warnings=render.Warnings; break;
            case "bundle":
                var bundleSource=new ProjectStore().Load(options.Required("project")); var bundled=ProjectBundler.Bundle(bundleSource,options.Required("out-dir"),token,path=>{ using var image=new System.Drawing.Bitmap(path); _=image.Width; }); data=JsonOutput.Node(new{path=bundled.AbsolutePath,hash=bundled.Sha256}); break;
            case "session":return SessionCommands.Execute(options,requestId,token);
            case "open":data=JsonOutput.Node(DesktopLauncher.Open(options.Required("project"),AppContext.BaseDirectory,options.Has("portable-data")));break;
            default: throw new EditorException("ARGS","Unknown command");
        }
        return new(JsonOutput.Envelope(requestId,true,"OK","Completed",data,JsonOutput.Node(changes.Select(c=>c.ObjectId).Distinct()),JsonOutput.Node(warnings)),0);
    }
}
