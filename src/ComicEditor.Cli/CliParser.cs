using System.Globalization;
using ComicEditor.Core.Validation;
namespace ComicEditor.Cli;
public sealed record CliArguments(string Command,string? Subcommand,IReadOnlyDictionary<string,string> Options)
{
    public bool Has(string key)=>Options.ContainsKey(key);
    public string? Get(string key)=>Options.GetValueOrDefault(key);
    public string Required(string key)=>Get(key)??throw new EditorException("ARGS","Missing --"+key);
    public double Number(string key,double fallback) => !Has(key)?fallback:double.TryParse(Get(key),NumberStyles.Float,CultureInfo.InvariantCulture,out var value)&&double.IsFinite(value)?value:throw new EditorException("ARGS","Invalid invariant number: --"+key);
}
public static class CliParser
{
    public static CliArguments Parse(string[] args)
    {
        var index=0; var command=args.Length==0?"capabilities":args[index++]; string? sub=null;
        if(command=="--help")command="help";
        if(command=="session") { if(index<args.Length&&args[index]=="--help")command="help";else {if(index>=args.Length) throw new EditorException("ARGS","Session subcommand required"); sub=args[index++];} }
        var fields=command switch
        {
            "capabilities" or "help"=>Array.Empty<string>(),"inspect" or "validate"=>["project"],"init"=>["manifest","out"],"apply"=>["project","patch","out","dry-run","overwrite","if-output-hash"],
            "render"=>["project","out","scale","object","padding","memory-limit-mib","overwrite","if-output-hash"],"bundle"=>["project","out-dir"],"open"=>["project","portable-data"],
            "session"=>sub switch { "list"=>["registry-dir","portable-data"],"status" or "snapshot"=>["session","registry-dir","portable-data"],"apply"=>["session","patch","dry-run","registry-dir","portable-data"],"save"=>["session","out","revision","base-hash","if-output-hash","registry-dir","portable-data"],_=>throw new EditorException("ARGS","Unknown session subcommand") },
            _=>throw new EditorException("ARGS","Unknown command: "+command)
        };
        var allowed=fields.Concat(["request-id","report","timeout","help"]).ToHashSet(StringComparer.Ordinal); var flags=new[]{"dry-run","overwrite","portable-data","help"}; var options=new Dictionary<string,string>(StringComparer.Ordinal);
        for(;index<args.Length;index++)
        {
            var arg=args[index]; if(!arg.StartsWith("--",StringComparison.Ordinal)||!allowed.Contains(arg[2..])) throw new EditorException("ARGS","Unknown option: "+arg);
            var key=arg[2..]; if(options.ContainsKey(key)) throw new EditorException("ARGS","Duplicate --"+key);
            if(flags.Contains(key)) options[key]="true";
            else { if(++index>=args.Length||args[index].StartsWith("--",StringComparison.Ordinal)) throw new EditorException("ARGS","Value required for --"+key); options[key]=args[index]; }
        }
        if(options.TryGetValue("request-id",out var id)&&!Guid.TryParse(id,out _)) throw new EditorException("ARGS","request-id must be UUID");
        return new(command,sub,options);
    }
}
