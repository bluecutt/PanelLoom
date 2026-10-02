using ComicEditor.Tests;
namespace ComicEditor.UiTests;
public static class Program
{
    [STAThread] public static int Main(string[] args)
    {
        ComicEditor.Rendering.LegacyGdiBootstrap.Initialize();
        var suite=args.Length>1?args[1]:"All";
        var cases=typeof(Program).Assembly.GetTypes().Where(t=>typeof(ITestSuite).IsAssignableFrom(t)&&!t.IsInterface&&!t.IsAbstract).SelectMany(t=>((ITestSuite)Activator.CreateInstance(t)!).Cases()).Where(c=>suite is "All" or "UiAll"||c.Name.StartsWith(suite+".",StringComparison.Ordinal));
        return TestHarness.RunCases(suite,cases);
    }
}
