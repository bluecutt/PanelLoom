using System.Text;
using ComicEditor.Cli;
using ComicEditor.Rendering;
Console.OutputEncoding=new UTF8Encoding(false);
using var cancellation=new CancellationTokenSource();
Console.CancelKeyPress+=(_,e)=>{ e.Cancel=true; cancellation.Cancel(); };
CliResult result;
try { LegacyGdiBootstrap.Initialize(); result=await new CliDispatcher().RunAsync(args,cancellation.Token); }
catch(Exception ex) { result=new(JsonOutput.Envelope(null,false,"INTERNAL",ex.Message),70); }
Console.WriteLine(result.Envelope.ToJsonString(JsonOutput.Options));
return result.ExitCode;
