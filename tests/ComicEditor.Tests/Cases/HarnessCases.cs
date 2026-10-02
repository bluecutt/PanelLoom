using System.Diagnostics;

namespace ComicEditor.Tests;

public interface ITestSuite { IEnumerable<TestCase> Cases(); }
public sealed class HarnessCases : ITestSuite
{
    public IEnumerable<TestCase> Cases() => [
        new("Harness.UnknownSuiteFails", () => Assert.Equal(2, TestHarness.Run("NotARegisteredSuite", TextWriter.Null))),
        new("Harness.FailureIsNonZero", () => Assert.Equal(1, TestHarness.RunCases("Probe", [new("Probe.Throws", () => throw new Exception("expected probe failure"))], TextWriter.Null))),
        new("Harness.EmptySuiteCannotPass", () => Assert.Equal(2, TestHarness.RunCases("Empty", [], TextWriter.Null))),
        new("Harness.BaselineWritesOnlyToArtifacts", () => {
            var start = new ProcessStartInfo("powershell.exe") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
            foreach (var arg in new[] { "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", Path.GetFullPath("scripts/Capture-Legacy-Baseline.ps1"), "-OutputDirectory", Path.GetTempPath(), "-ValidateOnly" }) start.ArgumentList.Add(arg);
            using var process = Process.Start(start)!;
            var stdout = process.StandardOutput.ReadToEnd(); var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit(); Assert.True(process.ExitCode != 0 && (stdout + stderr).Contains("BASELINE_OUTPUT_SCOPE"), "Baseline did not explicitly reject output scope: " + stdout + stderr);
        })
    ];
}
