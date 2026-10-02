namespace ComicEditor.Tests;

public sealed record TestCase(string Name, Action Body);
public static class TestHarness
{
    public static int Run(string suite, TextWriter? output = null)
    {
        var cases = typeof(TestHarness).Assembly.GetTypes()
            .Where(t => typeof(ITestSuite).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .OrderBy(t => t.FullName, StringComparer.Ordinal)
            .SelectMany(t => ((ITestSuite)Activator.CreateInstance(t)!).Cases())
            .Where(c => suite == "All" || (suite=="Public"&&IsPublic(c.Name)) || c.Name.StartsWith(suite + ".", StringComparison.Ordinal)).ToArray();
        return RunCases(suite, cases, output);
    }
    private static bool IsPublic(string name)=>!name.Contains(".Private",StringComparison.Ordinal)&&!name.StartsWith("LegacyRender.",StringComparison.Ordinal)&&!name.StartsWith("Package.",StringComparison.Ordinal)&&name is not ("Assets.RealSourceDimensions" or "Project.P10RoundTrip" or "SingleLine.P10ExactGolden" or "Cli.P10OnlyOneBalloon");
    public static int RunCases(string suite, IEnumerable<TestCase> cases, TextWriter? output = null)
    {
        output ??= Console.Out;
        var list = cases.ToArray();
        if (list.Length == 0) { output.WriteLine($"SUITE={suite} PASSED=0 FAILED=0 ERROR=UNKNOWN_OR_EMPTY_SUITE"); return 2; }
        var failures = 0;
        foreach (var item in list)
        {
            try { item.Body(); output.WriteLine("PASS " + item.Name); }
            catch (Exception ex) { failures++; output.WriteLine($"FAIL {item.Name}: {ex.Message}"); }
        }
        output.WriteLine($"SUITE={suite} PASSED={list.Length-failures} FAILED={failures}");
        return failures == 0 ? 0 : 1;
    }
}

public static class Assert
{
    public static void True(bool condition, string message = "Expected true") { if (!condition) throw new Exception(message); }
    public static void Equal<T>(T expected, T actual) { if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"Expected {expected}; actual {actual}"); }
    public static T Throws<T>(Action action) where T : Exception { try { action(); } catch (T ex) { return ex; } throw new Exception($"Expected {typeof(T).Name}"); }
}
