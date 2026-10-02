namespace ComicEditor.Core.Assets;
public sealed record AssetInspection(string Kind,string ObjectId,string Reference,string AbsolutePath,int? Width,int? Height,string? Sha256,bool Missing,string? Error);
public sealed record AssetFingerprint(int Width,int Height,string Sha256,long Length,DateTime ModifiedUtc);
