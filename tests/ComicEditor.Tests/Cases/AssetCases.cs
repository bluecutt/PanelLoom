using System.Drawing;
using System.Text.Json.Nodes;
using ComicEditor.Core.Assets;
using ComicEditor.Core.Commands;
using ComicEditor.Core.Project;
using ComicEditor.Core.Validation;
namespace ComicEditor.Tests;
public sealed class AssetCases : ITestSuite
{
    public IEnumerable<TestCase> Cases()=>[
        new("Assets.ReplacePreservesOthers",()=>{ var doc=CommandCases.Scene(); var result=new CommandProcessor().Plan(doc,[CommandCases.Op("asset.replace","A","{\"kind\":\"panel\",\"sourceImage\":\"b.png\"}")]); var target=result.After.Root["panels"]![0]!.DeepClone(); target["sourceImage"]="art.png"; Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![0],target)); Assert.True(JsonNode.DeepEquals(doc.Root["panels"]![1],result.After.Root["panels"]![1])); Assert.True(JsonNode.DeepEquals(doc.Root["balloons"],result.After.Root["balloons"])); Assert.Equal(20,new AssetResolver().Inspect(result.After).First().Width); }),
        new("Assets.ChangedDimensionsRequireExplicitPolicy",()=>{ var doc=CommandCases.Scene(); var file=ProjectPaths.Resolve(doc,"wide.png"); using(var image=new Bitmap(40,20)) image.Save(file); Assert.Equal("FIT_POLICY_REQUIRED",Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[CommandCases.Op("asset.replace","A","{\"kind\":\"panel\",\"sourceImage\":\"wide.png\"}")])).Code); new CommandProcessor().Plan(doc,[CommandCases.Op("asset.replace","A","{\"kind\":\"panel\",\"sourceImage\":\"wide.png\",\"fitPolicy\":\"preserve\"}")]); }),
        new("Assets.SourceChangedInvalidatesCache",()=>{ var doc=CommandCases.Scene(); var resolver=new AssetResolver(); var before=resolver.Inspect(doc).First(); using(var image=new Bitmap(30,25)) image.Save(before.AbsolutePath); var after=resolver.Inspect(doc).First(); Assert.True(before.Sha256!=after.Sha256); Assert.Equal(30,after.Width); Assert.Equal(25,after.Height); }),
        new("Assets.ChinesePathAndMetadata",()=>{ var doc=CommandCases.Scene(); var file=ProjectPaths.Resolve(doc,"中文 image.png"); using(var image=new Bitmap(23,27)) image.Save(file); doc.Root["panels"]![0]!["sourceImage"]="中文 image.png"; var item=new AssetResolver().Inspect(doc).First(); Assert.Equal(23,item.Width); Assert.Equal(27,item.Height); Assert.True(!item.Missing); Assert.Equal(64,item.Sha256!.Length); }),
        new("Assets.MissingSourceIsObjectSpecific",()=>{ var doc=CommandCases.Scene(); doc.Root["panels"]![0]!["sourceImage"]="missing.png"; var missing=new AssetResolver().Inspect(doc).First(); Assert.Equal("A",missing.ObjectId); Assert.True(missing.Missing); Assert.True(missing.Error is not null); }),
        new("Assets.MissingReplacementRejectsBeforeMutation",()=>{ var doc=CommandCases.Scene(); var before=doc.Root.ToJsonString(); Assert.Throws<EditorException>(()=>new CommandProcessor().Plan(doc,[CommandCases.Op("asset.replace","A","{\"kind\":\"panel\",\"sourceImage\":\"missing.png\"}")])); Assert.Equal(before,doc.Root.ToJsonString()); }),
        new("Assets.RealSourceDimensions",()=>{ foreach(var name in new[]{"P09Legacy","P10SingleLine"}) { var doc=new ProjectStore().Load("artifacts/private-fixtures/"+name+".json").Document; var assets=new AssetResolver().Inspect(doc); Assert.True(assets.All(a=>!a.Missing&&a.Error is null&&a.Width>0&&a.Height>0)); Console.WriteLine($"ASSET_REPORT {name} objects={assets.Count} sizes={string.Join(";",assets.Select(a=>$"{a.ObjectId}={a.Width}x{a.Height}"))}"); } })
    ];
}
