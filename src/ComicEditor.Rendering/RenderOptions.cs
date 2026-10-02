namespace ComicEditor.Rendering;
public sealed record RenderOptions(double Scale=0,string? ObjectId=null,double Padding=0,long MemoryLimitBytes=1024L*1024*1024,bool Overwrite=false,string? ExpectedDestinationHash=null);
